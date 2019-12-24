﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Mono.Cecil;
using NLog;
using Phase.CompilerServices;

namespace Phase.Translator
{
    class AttributeLoader : CSharpSyntaxWalker
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ConcurrentDictionary<SyntaxTree, SemanticModel> _semanticModel;
        private readonly INamedTypeSymbol _compilerExtensionType;
        private readonly INamedTypeSymbol _compilerContextType;
        private CancellationToken _cancellationToken;

        public CSharpCompilation Compilation { get; private set; }
        public AttributeRegistry Attributes { get; private set; }

        public AttributeLoader(PhaseLanguage language, CSharpCompilation compilation)
        {
            Compilation = compilation;
            switch (language)
            {
                case PhaseLanguage.Haxe:
                    _compilerExtensionType = compilation.GetTypeByMetadataName("Phase.CompilerServices.IHaxeCompilerExtension");
                    break;
                case PhaseLanguage.Cpp:
                    _compilerExtensionType = compilation.GetTypeByMetadataName("Phase.CompilerServices.ICppCompilerExtension");
                    break;
                case PhaseLanguage.Kotlin:
                    _compilerExtensionType = compilation.GetTypeByMetadataName("Phase.CompilerServices.IKotlinCompilerExtension");
                    break;
                case PhaseLanguage.TypeScript:
                    _compilerExtensionType = compilation.GetTypeByMetadataName("Phase.CompilerServices.ITypeScriptCompilerExtension");
                    break;
            }
            _compilerContextType = compilation.GetTypeByMetadataName("Phase.CompilerServices.ICompilerContext");
        }

        public async Task LoadAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            
            // add all compiler extensions to the project
            var trees = new ConcurrentBag<SyntaxTree>();
            Parallel.ForEach(Compilation.References.OfType<PortableExecutableReference>(), r =>
            {
                foreach (var source in LoadCompilerExtensionsFromAssembly(r.FilePath))
                {
                    try
                    {
                        trees.Add(CSharpSyntaxTree.ParseText(source.source, CSharpParseOptions.Default.WithLanguageVersion(Compilation.LanguageVersion)));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Failed to parse C# source from resource {0} in assembly {1}", source.name, r.FilePath);
                    }
                }
            });

            var assemblyName = Compilation.AssemblyName.EndsWith(".dll")
                ? Compilation.AssemblyName.Substring(0, Compilation.AssemblyName.Length - 4)
                : Compilation.AssemblyName;
            
            Compilation = Compilation
                .AddSyntaxTrees(trees)
                .WithAssemblyName(assemblyName);

            _semanticModel = new ConcurrentDictionary<SyntaxTree, SemanticModel>(SyntaxTreeComparer.Instance);

            Attributes = new AttributeRegistry();
            LoadAttributes(Compilation, cancellationToken);

            foreach (var reference in Compilation.References.OfType<CompilationReference>())
            {
                LoadAttributes(reference.Compilation, cancellationToken);
            }

            Attributes = Attributes;
        }

        private void LoadAttributes(Compilation compilation, CancellationToken cancellationToken)
        {
            Parallel.ForEach(compilation.SyntaxTrees, syntaxTree =>
            {
                var root = syntaxTree.GetRoot(cancellationToken);
                _semanticModel[syntaxTree] = compilation.GetSemanticModel(syntaxTree);
                Visit(root);
            });
        }

        private IEnumerable<(string name, string source)> LoadCompilerExtensionsFromAssembly(string path)
        {
            try
            {
                var fileName = Path.GetFileName(path);
                if (fileName == "mscorlib.dll" || fileName.StartsWith("System."))
                {
                    return Enumerable.Empty<(string, string)>();
                }

                Log.Trace($"Loading resources from {path}");
                var assembly = AssemblyDefinition.ReadAssembly(path);
                return assembly.MainModule.Resources.OfType<EmbeddedResource>()
                    .Where(r => r.Name.EndsWith("CompilerExtension.cs"))
                    .Select(ReadResource);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to load resources from {0}", path);
                return Enumerable.Empty<(string, string)>();
            }
        }

        private (string, string) ReadResource(EmbeddedResource resource)
        {
            using (var s = new StreamReader(resource.GetResourceStream()))
            {
                return (resource.Name, s.ReadToEnd());
            }
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var type = _semanticModel[node.SyntaxTree].GetDeclaredSymbol(node, _cancellationToken);
            var isExtension = type.Interfaces.Any(i => i.Equals(_compilerExtensionType));
            if (isExtension)
            {
                Log.Info($"Found compiler extension class '{type.Name}'");
                foreach (var member in node.Members)
                {
                    if (member.Kind() == SyntaxKind.MethodDeclaration)
                    {
                        VisitMethodDeclaration((MethodDeclarationSyntax)member);
                    }
                }
            }
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var semanticModel = _semanticModel[node.SyntaxTree];
            if (node.Identifier.ValueText == "Run" || node.Identifier.ValueText == "ICompilerExtension.Run")
            {
                var method = semanticModel.GetDeclaredSymbol(node, _cancellationToken);
                {
                    if (method.Parameters.Length != 1 ||
                        !method.Parameters[0].Type.Equals(_compilerContextType))
                    {
                        Compilation.GetDiagnostics().Add(Diagnostic.Create(PhaseErrors.PH001,
                            node.ParameterList.GetLocation(),
                            method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                        return;
                    }

                    if (node.Body == null)
                    {
                        Compilation.GetDiagnostics().Add(Diagnostic.Create(PhaseErrors.PH003,
                            node.ParameterList.GetLocation(),
                            method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                        return;
                    }

                    var interpreter = new CompilerExtensionInterpreter(Attributes, semanticModel, node.Body);
                    interpreter.Execute();
                }
            }
        }
    }

    class CompilerExtensionInterpreter : CSharpSyntaxWalker
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly AttributeRegistry Attributes;
        private readonly SemanticModel _semanticModel;
        private readonly BlockSyntax _body;

        private readonly Dictionary<string, AttributeBuilderDetails> _variables;

        private readonly ITypeSymbol AttributesBuilderType;
        private readonly ITypeSymbol AttributesContextType;
        private readonly INamedTypeSymbol _attributeTargetType;

        private class AttributeBuilderDetails
        {
            public ISymbol Symbol { get; set; }
            public AttributeTarget Target { get; set; }
        }

        public CompilerExtensionInterpreter(AttributeRegistry attributes, SemanticModel semanticModel, BlockSyntax body)
        {
            Attributes = attributes;
            _semanticModel = semanticModel;
            _body = body;
            _variables = new Dictionary<string, AttributeBuilderDetails>();
            AttributesBuilderType =
                semanticModel.Compilation.GetTypeByMetadataName("Phase.CompilerServices.IAttributesBuilder");
            AttributesContextType =
                semanticModel.Compilation.GetTypeByMetadataName("Phase.CompilerServices.IAttributesContext");
            _attributeTargetType =
                semanticModel.Compilation.GetTypeByMetadataName("Phase.CompilerServices.AttributeTarget");
        }

        public void Execute()
        {
            try
            {
                Visit(_body);
            }
            catch (InvalidDataException)
            {
            }
        }

        public override void VisitBlock(BlockSyntax node)
        {
            foreach (var statementSyntax in node.Statements)
            {
                Visit(statementSyntax);
            }
        }

        public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            var type = _semanticModel.GetTypeInfo(node.Type).Type;

            if (type.Equals(AttributesBuilderType))
            {
                foreach (var variable in node.Variables)
                {
                    this._variables[variable.Identifier.ValueText] = GetAttributesBuilderVariable(variable.Initializer.Value);
                }
            }
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var leftSymbol = _semanticModel.GetSymbolInfo(node.Left).Symbol;
            if (leftSymbol == null || leftSymbol.Kind != SymbolKind.Local)
            {
                return;
            }
            _variables[leftSymbol.Name] = GetAttributesBuilderVariable(node.Right);
        }

        private AttributeBuilderDetails GetAttributesBuilderVariable(ExpressionSyntax variableInitializer)
        {
            if (variableInitializer.Kind() == SyntaxKind.InvocationExpression)
            {
                InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)variableInitializer;

                var invokedSymbol = _semanticModel.GetSymbolInfo(invocation).Symbol;
                if (invokedSymbol?.ContainingType == null || !invokedSymbol.ContainingType.Equals(AttributesContextType))
                {
                    return null;
                }
                switch (invokedSymbol.Name)
                {
                    case "Assembly":
                        {
                            IAssemblySymbol assembly;
                            if (invocation.ArgumentList.Arguments.Count == 0)
                            {
                                assembly = _semanticModel.Compilation.Assembly;
                            }
                            else
                            {
                                var firstArgument = _semanticModel.GetConstantValue(invocation.ArgumentList.Arguments[0]);
                                if (!firstArgument.HasValue)
                                {
                                    throw ReportError(Diagnostic.Create(PhaseErrors.PH004,
                                         invocation.ArgumentList.Arguments[0].GetLocation(), ((IMethodSymbol)invokedSymbol).Parameters[0].Name));

                                }

                                var assemblyName = firstArgument.ToString();
                                var assemblies = _semanticModel.Compilation.References
                                    .Select(r => _semanticModel.Compilation.GetAssemblyOrModuleSymbol(r))
                                    .Where(r => r.Kind == SymbolKind.Assembly)
                                    .ToArray();

                                if (_semanticModel.Compilation.Assembly.Name == assemblyName)
                                {
                                    assembly = _semanticModel.Compilation.Assembly;
                                }
                                else
                                {

                                    assembly = (IAssemblySymbol)assemblies.FirstOrDefault(r => r.Name == assemblyName);
                                }


                                if (assembly == null)
                                {
                                    throw ReportError(Diagnostic.Create(PhaseErrors.PH005,
                                        invocation.ArgumentList.Arguments[0].GetLocation(),
                                        assemblyName, string.Join(", ", assemblies.Select(a => a.Name))
                                        ));

                                }
                            }

                            return new AttributeBuilderDetails
                            {
                                Symbol = assembly,
                                Target = AttributeTarget.Default
                            };
                        }
                    case "Type":
                        {
                            var method = (IMethodSymbol)invokedSymbol;
                            ITypeSymbol type;
                            if (method.TypeArguments.Length == 1)
                            {
                                type = method.TypeArguments[0].OriginalDefinition ?? method.TypeArguments[0];
                            }
                            else
                            {
                                var typeofExpr = invocation.ArgumentList.Arguments[0];
                                if (typeofExpr.Expression.Kind() != SyntaxKind.TypeOfExpression)
                                {
                                    throw ReportError(Diagnostic.Create(PhaseErrors.PH016,
                                        invocation.ArgumentList.Arguments[0].GetLocation()
                                    ));
                                }
                                type = _semanticModel.GetTypeInfo(((TypeOfExpressionSyntax)typeofExpr.Expression).Type).Type;
                            }

                            return new AttributeBuilderDetails
                            {
                                Symbol = type,
                                Target = AttributeTarget.Default
                            };
                        }
                    case "Member":
                        {
                            if (!(invocation.ArgumentList.Arguments[0].Expression is LambdaExpressionSyntax lambda)
                                || lambda.Body == null || lambda.Body.Kind() == SyntaxKind.Block)
                            {
                                throw ReportError(Diagnostic.Create(PhaseErrors.PH006,
                                    invocation.ArgumentList.Arguments[0].GetLocation()
                                ));

                            }

                            AttributeBuilderDetails details = new AttributeBuilderDetails();

                            var member = _semanticModel.GetSymbolInfo(lambda.Body).Symbol;
                            details.Symbol = member.OriginalDefinition ?? member;

                            string targetName = null;
                            for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                            {
                                var argument = invocation.ArgumentList.Arguments[i];
                                var type = _semanticModel.GetTypeInfo(argument.Expression).Type;

                                var value = _semanticModel.GetConstantValue(argument.Expression);
                                if (!value.HasValue)
                                {
                                    throw ReportError(Diagnostic.Create(PhaseErrors.PH004,
                                        invocation.ArgumentList.Arguments[0].GetLocation(), ((IMethodSymbol)invokedSymbol).Parameters[i].Name));

                                }

                                if (type.Equals(_attributeTargetType))
                                {
                                    details.Target = (AttributeTarget)(int)value.Value;
                                }
                                else if (type.SpecialType == SpecialType.System_String)
                                {
                                    targetName = value.Value.ToString();
                                }
                            }

                            if (targetName != null)
                            {
                                if (details.Target == AttributeTarget.Parameter)
                                {
                                    if (details.Symbol.Kind == SymbolKind.Method)
                                    {
                                        details.Symbol =
                                            ((IMethodSymbol)details.Symbol).Parameters.FirstOrDefault(p => p.Name == targetName);
                                    }
                                }
                            }

                            switch (details.Target)
                            {
                                case AttributeTarget.Default:
                                    break;
                                case AttributeTarget.ReturnValue:
                                    if (details.Symbol.Kind != SymbolKind.Method)
                                    {
                                        throw ReportError(Diagnostic.Create(PhaseErrors.PH004,
                                            invocation.ArgumentList.GetLocation(), details.Target, SymbolKind.Property));

                                    }
                                    break;
                                case AttributeTarget.Parameter:
                                    if (details.Symbol == null)
                                    {
                                        var parameterList = member.Kind == SymbolKind.Method
                                            ? ((IMethodSymbol)member).Parameters.Select(p => p.Name)
                                            : Enumerable.Empty<string>();

                                        throw ReportError(Diagnostic.Create(PhaseErrors.PH008,
                                            invocation.ArgumentList.GetLocation(),
                                            member.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                                            targetName, string.Join(", ", parameterList)));

                                    }
                                    break;
                                case AttributeTarget.Getter:
                                    {
                                        if (details.Symbol.Kind != SymbolKind.Property)
                                        {
                                            throw ReportError(Diagnostic.Create(PhaseErrors.PH004,
                                                invocation.ArgumentList.GetLocation(), details.Target, SymbolKind.Property));

                                        }

                                        details.Symbol = ((IPropertySymbol)details.Symbol).GetMethod;
                                    }
                                    break;
                                case AttributeTarget.Setter:
                                    {
                                        if (details.Symbol.Kind != SymbolKind.Property)
                                        {
                                            throw ReportError(Diagnostic.Create(PhaseErrors.PH004,
                                                invocation.ArgumentList.GetLocation(), details.Target, SymbolKind.Property));

                                        }

                                        details.Symbol = ((IPropertySymbol)details.Symbol).SetMethod;
                                    }
                                    break;
                                case AttributeTarget.Adder:
                                    {
                                        if (details.Symbol.Kind != SymbolKind.Event)
                                        {
                                            throw ReportError(Diagnostic.Create(PhaseErrors.PH004,
                                                invocation.ArgumentList.GetLocation(), details.Target, SymbolKind.Event));

                                        }

                                        details.Symbol = ((IEventSymbol)details.Symbol).AddMethod;
                                    }
                                    break;
                                case AttributeTarget.Remover:
                                    {
                                        if (details.Symbol.Kind != SymbolKind.Event)
                                        {
                                            throw ReportError(Diagnostic.Create(PhaseErrors.PH004,
                                                invocation.ArgumentList.GetLocation(), details.Target, SymbolKind.Event));

                                        }

                                        details.Symbol = ((IEventSymbol)details.Symbol).RemoveMethod;
                                    }
                                    break;
                            }

                            return details;
                        }
                    case "Constructor":
                        {
                            if (!(invocation.ArgumentList.Arguments[0].Expression is LambdaExpressionSyntax lambda)
                                || lambda.Body == null || lambda.Body.Kind() == SyntaxKind.Block)
                            {
                                throw ReportError(Diagnostic.Create(PhaseErrors.PH006,
                                    invocation.ArgumentList.Arguments[0].GetLocation()
                                ));
                            }

                            var member = _semanticModel.GetSymbolInfo(lambda.Body).Symbol;
                            if (member.Kind != SymbolKind.Method ||
                                ((IMethodSymbol)member).MethodKind != MethodKind.Constructor)
                            {
                                throw ReportError(Diagnostic.Create(PhaseErrors.PH009,
                                    invocation.ArgumentList.Arguments[0].GetLocation()));

                            }

                            AttributeBuilderDetails details = new AttributeBuilderDetails();
                            details.Symbol = member.OriginalDefinition ?? member;

                            return details;
                        }
                    case "Event":
                        {
                            var eventName = _semanticModel.GetConstantValue(invocation.ArgumentList.Arguments[0].Expression);
                            if (!eventName.HasValue)
                            {
                                throw ReportError(Diagnostic.Create(PhaseErrors.PH004,
                                    invocation.ArgumentList.Arguments[0].GetLocation(), ((IMethodSymbol)invokedSymbol).Parameters[0].Name));

                            }

                            var type = ((IMethodSymbol)invokedSymbol).TypeArguments[0];
                            var events = type.GetMembers()
                                .Where(m => m.Kind == SymbolKind.Event);
                            var eventSymbol = events.FirstOrDefault(e => e.Name == eventName.Value.ToString());
                            if (!eventName.HasValue)
                            {
                                throw ReportError(Diagnostic.Create(PhaseErrors.PH010,
                                    invocation.ArgumentList.Arguments[0].GetLocation(), eventName.Value,
                                    type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                                    string.Join(", ", events.Select(e => e.Name))));

                            }


                            AttributeBuilderDetails details = new AttributeBuilderDetails();
                            details.Symbol = eventSymbol.OriginalDefinition ?? eventSymbol;

                            for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                            {
                                var argument = invocation.ArgumentList.Arguments[i];
                                var argumentType = _semanticModel.GetTypeInfo(argument).Type;

                                var value = _semanticModel.GetConstantValue(argument.Expression);
                                if (!value.HasValue)
                                {
                                    throw ReportError(Diagnostic.Create(PhaseErrors.PH004,
                                        invocation.ArgumentList.Arguments[0].GetLocation(), ((IMethodSymbol)invokedSymbol).Parameters[i].Name));

                                }

                                if (argumentType.Equals(_attributeTargetType))
                                {
                                    details.Target = (AttributeTarget)(int)value.Value;
                                }
                            }

                            switch (details.Target)
                            {
                                case AttributeTarget.Default:
                                    break;
                                case AttributeTarget.Adder:
                                    {
                                        if (details.Symbol.Kind != SymbolKind.Event)
                                        {
                                            throw ReportError(Diagnostic.Create(PhaseErrors.PH004,
                                                invocation.ArgumentList.GetLocation(), details.Target, SymbolKind.Event));
                                        }

                                        details.Symbol = ((IEventSymbol)details.Symbol).AddMethod;
                                    }
                                    break;
                                case AttributeTarget.Remover:
                                    {
                                        if (details.Symbol.Kind != SymbolKind.Event)
                                        {
                                            throw ReportError(Diagnostic.Create(PhaseErrors.PH004,
                                                invocation.ArgumentList.GetLocation(), details.Target, SymbolKind.Event));
                                        }

                                        details.Symbol = ((IEventSymbol)details.Symbol).RemoveMethod;
                                    }
                                    break;
                            }

                            return details;
                        }
                }
            }

            return null;
        }

        private Exception ReportError(Diagnostic diagnostic)
        {
            _semanticModel.Compilation.GetDiagnostics().Add(diagnostic);
            return new InvalidDataException();
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var invokedMethod = (IMethodSymbol)_semanticModel.GetSymbolInfo(node).Symbol;

            if (invokedMethod.ContainingType.Equals(AttributesBuilderType))
            {
                HandleAttributeBuilderInvocation(node, invokedMethod);
            }
            else
            {
                throw ReportError(Diagnostic.Create(PhaseErrors.PH011, node.GetLocation()));
            }
        }

        private void HandleAttributeBuilderInvocation(InvocationExpressionSyntax node, IMethodSymbol method)
        {
            // add attribute
            if (method.Name == "Add" && node.Expression.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                var memberAccess = (MemberAccessExpressionSyntax)node.Expression;

                AttributeBuilderDetails variableDetails = null;
                if (memberAccess.Expression.Kind() == SyntaxKind.InvocationExpression)
                {
                    // chained method calls
                    variableDetails = this.GetAttributesBuilderVariable(memberAccess.Expression);
                }
                else
                {
                    var symbol = _semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
                    if (symbol != null && symbol.Kind == SymbolKind.Local)
                    {
                        // variable access
                        _variables.TryGetValue(symbol.Name, out variableDetails);
                    }
                }

                if (variableDetails == null)
                {
                    throw ReportError(Diagnostic.Create(PhaseErrors.PH012, node.Expression.GetLocation()));
                }

                foreach (var argument in node.ArgumentList.Arguments)
                {
                    if (argument.Expression.Kind() != SyntaxKind.ObjectCreationExpression)
                    {
                        throw ReportError(Diagnostic.Create(PhaseErrors.PH013, argument.GetLocation()));
                    }

                    var newAttribute = (ObjectCreationExpressionSyntax)argument.Expression;
                    var constructor = (IMethodSymbol)_semanticModel.GetSymbolInfo(newAttribute).Symbol;

                    var constructorArguments = new List<TypedConstant>();
                    var namedArguments = new Dictionary<string, TypedConstant>();

                    for (var i = 0; i < newAttribute.ArgumentList.Arguments.Count; i++)
                    {
                        var ctorArgument = newAttribute.ArgumentList.Arguments[i];
                        var typeInfo = _semanticModel.GetTypeInfo(ctorArgument.Expression);
                        var value = _semanticModel.GetConstantValue(ctorArgument.Expression);
                        if (!value.HasValue)
                        {
                            throw ReportError(Diagnostic.Create(PhaseErrors.PH004,
                                ctorArgument.GetLocation(), constructor.Parameters[i].Name));
                        }
                        constructorArguments.Add(CreateTypedConstant(typeInfo.Type, value.Value));
                    }

                    if (newAttribute.Initializer != null)
                    {
                        foreach (var initializerStatement in newAttribute.Initializer.Expressions)
                        {
                            if (initializerStatement.Kind() != SyntaxKind.SimpleAssignmentExpression)
                            {
                                throw ReportError(Diagnostic.Create(PhaseErrors.PH014, initializerStatement.GetLocation()));
                            }

                            var assignment = (AssignmentExpressionSyntax)initializerStatement;
                            var symbol = _semanticModel.GetSymbolInfo(assignment.Left).Symbol;
                            var typeInfo = _semanticModel.GetTypeInfo(assignment.Right);
                            var value = _semanticModel.GetConstantValue(assignment.Right);

                            if (!value.HasValue)
                            {
                                throw ReportError(Diagnostic.Create(PhaseErrors.PH004,
                                    assignment.Right.GetLocation(), symbol.Name));
                            }

                            namedArguments[symbol.Name] = CreateTypedConstant(typeInfo.Type, value.Value);
                        }
                    }

                    this.RegisterAttribute(variableDetails, new CustomAttributeData(constructor, constructorArguments, namedArguments)); ;
                }
            }
            else
            {
                Debug.Fail("Missing handling of method");
            }
        }

        private TypedConstant CreateTypedConstant(ITypeSymbol type, object value)
        {
            var constructor = typeof(TypedConstant).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .First(c => c.GetParameters().Length == 3);
            return (TypedConstant)constructor.Invoke(new object[] { type, GetTypedConstantKind(type), value });
        }

        private static TypedConstantKind GetTypedConstantKind(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Object:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                    return TypedConstantKind.Primitive;
                default:
                    switch (type.TypeKind)
                    {
                        case TypeKind.Array:
                            return TypedConstantKind.Array;
                        case TypeKind.Enum:
                            return TypedConstantKind.Enum;
                        case TypeKind.Error:
                            return TypedConstantKind.Error;
                        default:
                            return TypedConstantKind.Type;
                    }
            }
        }



        class CustomAttributeData : AttributeData
        {
            protected override INamedTypeSymbol CommonAttributeClass => CommonAttributeConstructor.ContainingType;
            protected override IMethodSymbol CommonAttributeConstructor { get; }
            protected override SyntaxReference CommonApplicationSyntaxReference { get; }
            protected override ImmutableArray<TypedConstant> CommonConstructorArguments { get; }
            protected override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments { get; }

            public CustomAttributeData(IMethodSymbol constructor, List<TypedConstant> constructorArguments,
                Dictionary<string, TypedConstant> namedArguments)
            {
                CommonAttributeConstructor = constructor;
                CommonConstructorArguments = constructorArguments.ToImmutableArray();
                CommonApplicationSyntaxReference = null;
                CommonNamedArguments = namedArguments.ToImmutableArray();
            }
        }

        private void RegisterAttribute(AttributeBuilderDetails variableDetails, AttributeData attribute)
        {
            Log.Info($"Registering attribute '{attribute.AttributeClass}' on element '{variableDetails.Symbol}'");
            switch (variableDetails.Target)
            {
                case AttributeTarget.ReturnValue:
                    Attributes.RegisterReturnValueAttribute(variableDetails.Symbol, attribute);
                    break;
                default:
                    Attributes.RegisterAttribute(variableDetails.Symbol, attribute);
                    break;
            }
        }
    }

    internal class SyntaxTreeComparer : IEqualityComparer<SyntaxTree>
    {
        public static readonly SyntaxTreeComparer Instance = new SyntaxTreeComparer();

        public bool Equals(SyntaxTree x, SyntaxTree y)
        {
            if (x == null)
                return y == null;
            if (y == null || !string.Equals(x.FilePath, y.FilePath, StringComparison.OrdinalIgnoreCase))
                return false;
            return SourceTextComparer.Instance.Equals(x.GetText(new CancellationToken()), y.GetText(new CancellationToken()));
        }

        public int GetHashCode(SyntaxTree obj)
        {
            return Hash.Combine(obj.FilePath.GetHashCode(), SourceTextComparer.Instance.GetHashCode(obj.GetText(new CancellationToken())));
        }
    }
    internal class SourceTextComparer : IEqualityComparer<SourceText>
    {
        public static SourceTextComparer Instance = new SourceTextComparer();

        public bool Equals(SourceText x, SourceText y)
        {
            if (x == null)
                return y == null;
            if (y == null)
                return false;
            return x.ContentEquals(y);
        }

        public int GetHashCode(SourceText obj)
        {
            ImmutableArray<byte> checksum = obj.GetChecksum();
            int newKey1 = !checksum.IsDefault ? Hash.CombineValues<byte>(checksum, int.MaxValue) : 0;
            int newKey2 = obj.Encoding != null ? obj.Encoding.GetHashCode() : 0;
            return Hash.Combine(obj.Length, Hash.Combine(newKey1, Hash.Combine(newKey2, obj.ChecksumAlgorithm.GetHashCode())));
        }
    }

    internal static class Hash
    {
        internal static int Combine(int newKey, int currentKey)
        {
            return unchecked((currentKey * (int)0xA5555529) + newKey);
        }
        internal static int CombineValues<T>(IEnumerable<T> values, int maxItemsToHash = int.MaxValue)
        {
            if (values == null)
            {
                return 0;
            }

            var hashCode = 0;
            var count = 0;
            foreach (var value in values)
            {
                if (count++ >= maxItemsToHash)
                {
                    break;
                }

                // Should end up with a constrained virtual call to object.GetHashCode (i.e. avoid boxing where possible).
                if (value != null)
                {
                    hashCode = Hash.Combine(value.GetHashCode(), hashCode);
                }
            }

            return hashCode;
        }

    }
}


using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using Phase.Translator.Cpp.Expressions;

namespace Phase.Translator.Cpp
{
    public abstract class AbstractCppEmitterBlock : AbstractEmitterBlock
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public CppEmitterContext EmitterContext { get; set; }

        protected override IWriter Writer => EmitterContext.Writer;
        public CppEmitter Emitter => EmitterContext.Emitter;

        public void PushWriter()
        {
            EmitterContext.PushWriter();
        }

        public string PopWriter()
        {
            return EmitterContext.PopWriter();
        }

        public void Init(CppEmitterContext context)
        {
            EmitterContext = context;
        }

        private static readonly Regex NewLine = new Regex("\r?\n", RegexOptions.Compiled);

        protected void WriteComments(SyntaxNode node, bool leading = true)
        {
            var trivia = leading
                    ? (node.HasLeadingTrivia ? node.GetLeadingTrivia() : default(SyntaxTriviaList))
                    : (node.HasTrailingTrivia ? node.GetTrailingTrivia() : default(SyntaxTriviaList))
                ;

            if (trivia.Any())
            {
                foreach (var t in trivia)
                {
                    var s = t.ToFullString();

                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        var lines = NewLine.Split(s.Trim());
                        foreach (var line in lines)
                        {
                            var trimmed = line.Trim();
                            if (trimmed.StartsWith("//"))
                            {
                                Write(trimmed);
                                WriteNewLine();
                            }
                        }
                    }
                }
                IsNewLine = true;
            }
        }

        protected void WriteComments(ISymbol node, CancellationToken cancellationToken)
        {
            WriteComments(node, true, cancellationToken);
        }

        protected void WriteComments(ISymbol node, bool leading, CancellationToken cancellationToken)
        {
            foreach (var declaration in node.DeclaringSyntaxReferences)
            {
                var syntax = declaration.GetSyntax(cancellationToken);
                WriteComments(syntax, leading);
            }
        }

        protected void WriteType(TypeSyntax syntax)
        {
            WriteType(Emitter.GetTypeSymbol(syntax));
        }

        protected void WriteType(ITypeSymbol type)
        {
            Write(Emitter.GetTypeName(type));
        }

        protected void WriteEventType(INamedTypeSymbol delegateType)
        {
            var delegateMethod = delegateType.DelegateInvokeMethod;

            Write("System::Event");
            if (delegateMethod.ReturnsVoid)
            {
                Write("Action<");
            }
            else
            {
                Write("Func<");
                WriteType(delegateMethod.ReturnType);
                Write(", ");
            }

            WriteType(delegateType);

            Write(">");
        }

        protected void WriteDefaultFileHeader()
        {
            Write("#include <phase.h>");
            WriteNewLine();

            Write("#include \"");
            Write(EmitterContext.CurrentType.SemanticModel.Compilation.Assembly.Name.Replace(".dll", "") + CppEmitter.FileExtensionHeader);
            Write("\"");
            WriteNewLine();

            WriteNewLine();
        }


        protected void WriteAccessibility(Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.NotApplicable:
                case Accessibility.Private:
                    WriteAccessibility("private");
                    break;
                case Accessibility.ProtectedAndInternal:
                case Accessibility.ProtectedOrInternal:
                case Accessibility.Internal:
                    WriteAccessibility("public");
                    // TODO: work with friend classes?
                    break;
                case Accessibility.Protected:
                    WriteAccessibility("protected");
                    break;
                case Accessibility.Public:
                    WriteAccessibility("public");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, null);
            }
        }

        protected void WriteAccessibility(string accessibility)
        {
            if (EmitterContext.PreviousAccessibility == accessibility) return;
            Writer.Outdent();
            WriteNewLine();
            Write(accessibility, ":");
            WriteNewLine();
            Writer.Indent();
            EmitterContext.PreviousAccessibility = accessibility;
        }

        public void test(string s = null)
        {

        }

        protected void WriteParameterDeclarations(ImmutableArray<IParameterSymbol> methodParameters, bool includeOptionals, CancellationToken cancellationToken)
        {
            for (int i = 0; i < methodParameters.Length; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                }

                var param = methodParameters[i];

                WriteType(methodParameters[i].Type);
                EmitterContext.ImportType(methodParameters[i].Type);

                WriteSpace();

                if (param.RefKind != RefKind.None)
                {
                    Write("&");
                }
                Write(param.Name);

                if (param.IsOptional && includeOptionals)
                {
                    Write(" = ");

                    var parameterSyntax = (ParameterSyntax)methodParameters[i].DeclaringSyntaxReferences.First().GetSyntax(cancellationToken);
                    if (!param.Type.IsReferenceType || param.Type.SpecialType == SpecialType.System_String)
                    {
                        EmitTree(parameterSyntax.Default.Value, cancellationToken);
                    }
                    else
                    {
                        if (parameterSyntax.Default.Value.Kind() == SyntaxKind.NullLiteralExpression)
                        {
                            Write(Emitter.GetTypeName(methodParameters[i].Type, false, false, CppEmitter.TypeNamePointerKind.SharedPointerDeclaration));
                            WriteOpenCloseParentheses();
                        }
                        else
                        {
                            Write("std::make_shared<");
                            Write(Emitter.GetTypeName(methodParameters[i].Type, false, false, CppEmitter.TypeNamePointerKind.NoPointer));
                            Write(">");
                            WriteOpenParentheses();
                            EmitTree(parameterSyntax.Default.Value, cancellationToken);
                            WriteCloseParentheses();
                        }
                    }
                }
            }
        }

        protected AbstractCppEmitterBlock EmitTree(SyntaxNode value, CancellationToken cancellationToken = default(CancellationToken))
        {
            var expressionBlock = new VisitorBlock(EmitterContext, value);
            expressionBlock.DoEmit(cancellationToken);
            return expressionBlock.FirstBlock;
        }

        protected void WriteInclude(string[] typeParts)
        {
            Write("#include \"", string.Join("/", typeParts), CppEmitter.FileExtensionHeader, "\"");
            WriteNewLine();
        }
        protected void WriteInclude(ITypeSymbol type)
        {
            if (type.SpecialType == SpecialType.System_Void) return;
            var fullName = Emitter.GetTypeName(type, false, true, CppEmitter.TypeNamePointerKind.NoPointer);
            var parts = fullName.Split(new[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
            WriteInclude(parts);
        }

        protected void WriteForwardDeclaration(ITypeSymbol importedType)
        {
            if (importedType.SpecialType != SpecialType.None)
            {
                WriteInclude(importedType);
                return;
            }

            if (importedType is INamedTypeSymbol named && named.IsGenericType)
            {
                WriteInclude(named);
                return;
            }

            var fullName = Emitter.GetTypeName(importedType, false, true, CppEmitter.TypeNamePointerKind.NoPointer);
            var parts = fullName.Split(new[] { "::" }, StringSplitOptions.RemoveEmptyEntries);

            var name = parts.Last();

            void WriteNamespaceOpen()
            {
                if (parts.Length > 1)
                {
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        Write("namespace ", parts[i], "{ ");
                    }
                }
            }

            void WriteNamespaceClose()
            {
                if (parts.Length > 1)
                {
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        Write(" }");
                    }
                }
                WriteNewLine();
            }


            switch (importedType.TypeKind)
            {
                case TypeKind.Enum:
                case TypeKind.Delegate:
                    // enums and delegates can be included directly
                    WriteInclude(parts);
                    break;
                case TypeKind.Array:
                    WriteForwardDeclaration(((IArrayTypeSymbol)importedType).ElementType);
                    break;
                case TypeKind.Class:
                case TypeKind.Interface:
                    WriteNamespaceOpen();
                    Write("class ", name);
                    WriteSemiColon();
                    WriteNamespaceClose();
                    WriteNewLine();
                    break;
                case TypeKind.Struct:
                    WriteNamespaceOpen();
                    Write("struct ", name);
                    WriteSemiColon();
                    WriteNamespaceClose();
                    break;
            }
        }

        protected void WriteMethodBody(IMethodSymbol _method, CancellationToken cancellationToken)
        {
            WriteNewLine();
            if (_method.DeclaringSyntaxReferences.IsEmpty && _method.MethodKind == MethodKind.Constructor && !_method.IsStatic && _method.ContainingType.BaseType != null && _method.ContainingType.BaseType.SpecialType != SpecialType.System_Object)
            {
                // default constructor 
                BeginBlock();
                EndBlock();
            }
            else if (!_method.DeclaringSyntaxReferences.IsEmpty)
            {
                foreach (var reference in _method.DeclaringSyntaxReferences)
                {
                    var node = reference.GetSyntax(cancellationToken);
                    var methodDeclarationSyntax = node as MethodDeclarationSyntax;
                    var constructorDeclarationSyntax = node as ConstructorDeclarationSyntax;
                    var accessorDeclarationSyntax = node as AccessorDeclarationSyntax;
                    var operatorDeclarationSyntax = node as OperatorDeclarationSyntax;
                    var conversionOperatorDeclarationSyntax = node as ConversionOperatorDeclarationSyntax;
                    var arrowExpressionClauseSyntax = node as ArrowExpressionClauseSyntax;
                    if (methodDeclarationSyntax != null)
                    {
                        BeginBlock();
                        if (methodDeclarationSyntax.ExpressionBody != null)
                        {
                            if (!_method.ReturnsVoid)
                            {
                                WriteReturn(true);
                            }
                            EmitTree(methodDeclarationSyntax.ExpressionBody.Expression,
                                cancellationToken);
                            WriteSemiColon(true);
                        }
                        else if (methodDeclarationSyntax.Body != null)
                        {
                            foreach (var statement in methodDeclarationSyntax.Body.Statements)
                            {
                                EmitTree(statement, cancellationToken);
                            }
                        }
                        EndBlock();
                    }
                    else if (conversionOperatorDeclarationSyntax != null)
                    {
                        BeginBlock();
                        if (conversionOperatorDeclarationSyntax.ExpressionBody != null)
                        {
                            if (!_method.ReturnsVoid)
                            {
                                WriteReturn(true);
                            }
                            EmitTree(conversionOperatorDeclarationSyntax.ExpressionBody.Expression,
                                cancellationToken);
                            WriteSemiColon(true);
                        }
                        else if (conversionOperatorDeclarationSyntax.Body != null)
                        {
                            foreach (var statement in conversionOperatorDeclarationSyntax.Body.Statements)
                            {
                                EmitTree(statement, cancellationToken);
                            }
                        }
                        EndBlock();
                    }
                    else if (operatorDeclarationSyntax != null)
                    {
                        BeginBlock();
                        if (operatorDeclarationSyntax.ExpressionBody != null)
                        {
                            if (!_method.ReturnsVoid)
                            {
                                WriteReturn(true);
                            }
                            EmitTree(operatorDeclarationSyntax.ExpressionBody.Expression,
                                cancellationToken);
                            WriteSemiColon(true);
                        }
                        else if (operatorDeclarationSyntax.Body != null)
                        {
                            foreach (var statement in operatorDeclarationSyntax.Body.Statements)
                            {
                                EmitTree(statement, cancellationToken);
                            }
                        }
                        EndBlock();
                    }
                    else if (arrowExpressionClauseSyntax != null)
                    {
                        BeginBlock();
                        if (!_method.ReturnsVoid)
                        {
                            WriteReturn(true);
                        }
                        EmitTree(arrowExpressionClauseSyntax.Expression,
                            cancellationToken);
                        WriteSemiColon(true);
                        EndBlock();
                    }
                    else if (constructorDeclarationSyntax != null)
                    {
                        var isFirstInitializer = true;

                        if (_method.IsStatic)
                        {
                            BeginBlock();
                        }
                        else
                        {
                            Indent();
                        }


                        if (constructorDeclarationSyntax.Initializer != null)
                        {
                            Write(": ");
                            var ctor = (IMethodSymbol)Emitter
                                .GetSymbolInfo(constructorDeclarationSyntax.Initializer)
                                .Symbol;

                            var x = Emitter.GetMethodName(ctor);
                            Write(x);

                            WriteMethodInvocation(ctor,
                                constructorDeclarationSyntax.Initializer.ArgumentList,
                                constructorDeclarationSyntax.Initializer,
                                cancellationToken);

                            isFirstInitializer = false;
                        }

                        // write default initializers
                        foreach (var members in _method.ContainingType.GetMembers()
                            .Where(f => f.IsStatic == _method.IsStatic))
                        {
                            bool hasInitializer = true;
                            ITypeSymbol memberType = null;
                            string memberName = string.Empty;
                            switch (members.Kind)
                            {
                                case SymbolKind.Field:
                                    var field = (IFieldSymbol)members;
                                    if (field.Type.IsValueType)
                                    {
                                        if (field.AssociatedSymbol?.Kind == SymbolKind.Property)
                                        {
                                            hasInitializer = false;
                                            memberType = field.Type;
                                            memberName = Emitter.GetFieldName(field);
                                        }
                                        else
                                        {
                                            foreach (var fieldReference in members.DeclaringSyntaxReferences)
                                            {
                                                if (fieldReference.GetSyntax(cancellationToken) is
                                                    VariableDeclaratorSyntax
                                                    declaration)
                                                {
                                                    hasInitializer = declaration.Initializer != null;
                                                    memberType = field.Type;
                                                    memberName = Emitter.GetFieldName(field);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    continue;
                            }

                            if (!hasInitializer)
                            {
                                WriteNewLine();

                                if (_method.IsStatic)
                                {
                                    var defaultValue = Emitter.GetDefaultValue(memberType);
                                    Write(memberName);
                                    Write(" = ");
                                    Write(defaultValue);
                                    WriteSemiColon(true);
                                }
                                else
                                {
                                    if (isFirstInitializer)
                                    {
                                        Write(": ");
                                    }
                                    else
                                    {
                                        Write(", ");
                                    }
                                    var defaultValue = Emitter.GetDefaultValue(memberType);
                                    Write(memberName);
                                    WriteOpenParentheses();
                                    Write(defaultValue);
                                    WriteCloseParentheses();
                                }

                                isFirstInitializer = false;
                            }
                        }

                        if (!_method.IsStatic)
                        {
                            Outdent();
                            WriteNewLine();
                            BeginBlock();
                        }

                        if (constructorDeclarationSyntax.ExpressionBody != null)
                        {
                            EmitTree(constructorDeclarationSyntax.ExpressionBody);
                            WriteSemiColon(true);
                        }

                        if (constructorDeclarationSyntax.Body != null)
                        {
                            foreach (var statement in constructorDeclarationSyntax.Body.Statements)
                            {
                                EmitTree(statement, cancellationToken);
                            }
                        }

                        EndBlock();
                    }
                    else if (accessorDeclarationSyntax != null)
                    {
                        BeginBlock();

                        if (accessorDeclarationSyntax.ExpressionBody != null)
                        {
                            if (!_method.ReturnsVoid || _method.MethodKind == MethodKind.PropertySet)
                            {
                                WriteReturn(true);
                            }
                            EmitTree(accessorDeclarationSyntax.ExpressionBody.Expression,
                                cancellationToken);
                            WriteSemiColon(true);
                        }
                        else if (accessorDeclarationSyntax.Body != null)
                        {
                            EmitterContext.SetterMethod =
                                _method.MethodKind == MethodKind.PropertySet ? _method : null;
                            foreach (var statement in accessorDeclarationSyntax.Body.Statements)
                            {
                                EmitTree(statement, cancellationToken);
                            }

                            EmitterContext.SetterMethod = null;

                            if (_method.MethodKind == MethodKind.PropertySet)
                            {
                                WriteReturn(true);
                                var property = (IPropertySymbol)_method.AssociatedSymbol;
                                if (property.GetMethod != null)
                                {
                                    Write(Emitter.GetMethodName(property.GetMethod));
                                    WriteOpenParentheses();
                                    if (property.IsIndexer)
                                    {
                                        for (int i = 0; i < property.GetMethod.Parameters.Length; i++)
                                        {
                                            if (i > 0)
                                            {
                                                WriteComma();
                                            }
                                            Write(property.GetMethod.Parameters[i].Name);
                                        }
                                    }
                                    WriteCloseParentheses();
                                }
                                else
                                {
                                    Write(_method.Parameters.Last().Name);
                                }
                                WriteSemiColon(true);
                            }
                        }
                        else
                        {
                            WriteDefaultImplementation(_method);
                        }

                        WriteNewLine();
                        EndBlock();
                    }
                    else
                    {
                        Debug.Fail($"Unhandled syntax node: {node.Kind()}");
                    }
                }
            }
            else
            {
                WriteDefaultImplementation(_method);
            }

            WriteComments(_method, false, cancellationToken);
        }


        protected void WriteMethodInvocation(IMethodSymbol method,
            ArgumentListSyntax argumentList,
            SyntaxNode callerNode = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteMethodInvocation(method, argumentList?.Arguments.Select(a => new ParameterInvocationInfo(a)), callerNode, cancellationToken);
        }

        protected void WriteMethodInvocation(IMethodSymbol method, IEnumerable<ParameterInvocationInfo> argumentList, SyntaxNode callerNode = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            EmitterContext.IsMethodInvocation = true;
            WriteOpenParentheses();

            EmitterContext.IsParameter = true;
            if (argumentList != null)
            {
                if (method == null)
                {
                    var args = argumentList.ToArray();
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (i > 0) WriteComma();
                        EmitTree(args[i].Expression, cancellationToken);
                    }
                }
                else
                {
                    EmitterContext.ImportType(method.ReturnType);

                    BaseMethodDeclarationSyntax methodDeclaration = null;
                    foreach (var reference in method.DeclaringSyntaxReferences)
                    {
                        methodDeclaration = reference.GetSyntax(cancellationToken) as BaseMethodDeclarationSyntax;
                        if (methodDeclaration != null)
                        {
                            break;
                        }
                    }

                    var arguments = BuildMethodInvocation(method, argumentList);
                    var isFirstParam = true;
                    foreach (var argument in argumentList.Where(a => a.InjectAtBeginning))
                    {
                        if (!isFirstParam) WriteComma();
                        isFirstParam = false;
                        EmitTree(argument.Expression, cancellationToken);
                    }

                    var isRawParams = Emitter.IsRawParams(method);

                    // print expressions
                    for (int i = 0; i < method.Parameters.Length; i++)
                    {
                        if (!isFirstParam) WriteComma();
                        isFirstParam = false;

                        var param = method.Parameters[i];
                        var value = arguments[param.Name].ToArray();
                        if (param.IsParams)
                        {
                            if (value.Length == 1)
                            {
                                var singleParamType = Emitter.GetTypeInfo(value[0]);
                                if (singleParamType.ConvertedType.Equals(param.Type))
                                {
                                    EmitTree(value[0], cancellationToken);
                                }
                                else
                                {
                                    if (!isRawParams)
                                    {
                                        Write("Phase::make_va_args<");
                                        if (param.Type is IArrayTypeSymbol array)
                                        {
                                            WriteType(array.ElementType);
                                            EmitterContext.ImportType(array.ElementType);
                                        }
                                        else if (param.Type is INamedTypeSymbol nt && nt.IsGenericType)
                                        {
                                            WriteType(nt.TypeArguments[0]);
                                            EmitterContext.ImportType(nt.TypeArguments[0]);
                                        }
                                        Write(">(");
                                    }
                                    EmitTree(value[0], cancellationToken);
                                    if (!isRawParams) Write(")");
                                }
                            }
                            else
                            {
                                if (!isRawParams)
                                {
                                    Write("Phase::make_va_args<");
                                    if (param.Type is IArrayTypeSymbol array)
                                    {
                                        WriteType(array.ElementType);
                                        EmitterContext.ImportType(array.ElementType);
                                    }
                                    else if (param.Type is INamedTypeSymbol nt && nt.IsGenericType)
                                    {
                                        WriteType(nt.TypeArguments[0]);
                                        EmitterContext.ImportType(nt.TypeArguments[0]);
                                    }
                                    Write(">(");
                                }
                                for (var j = 0; j < value.Length; j++)
                                {
                                    if (j > 0) WriteComma();
                                    EmitTree(value[j], cancellationToken);
                                }
                                if (!isRawParams) Write(")");
                            }
                        }
                        else
                        {
                            if (value.Length == 1)
                            {
                                EmitTree(value[0], cancellationToken);
                            }
                            else if (param.IsOptional)
                            {
                                if (EmitterContext.TryGetCallerMemberInfo(param, EmitterContext.CurrentMember, callerNode, out var callerValue))
                                {
                                    Write(callerValue);
                                }
                                else if (methodDeclaration != null)
                                {
                                    var parameterDeclaration = methodDeclaration.ParameterList.Parameters[i].Default.Value;
                                    EmitTree(parameterDeclaration, cancellationToken);
                                }
                                else if (param.HasExplicitDefaultValue && param.ExplicitDefaultValue != null)
                                {
                                    Write(param.ExplicitDefaultValue);
                                }
                                else
                                {
                                    Write("nullptr");
                                }
                            }
                        }
                    }
                }
            }
            EmitterContext.IsParameter = false;

            WriteCloseParentheses();

            EmitterContext.IsMethodInvocation = false;
        }

        private void WriteDefaultImplementation(IMethodSymbol method)
        {
            if (method.MethodKind == MethodKind.PropertyGet)
            {
                WriteAutoPropertyGetter(method);
            }
            else if (method.MethodKind == MethodKind.PropertySet)
            {
                WriteAutoPropertySetter(method);
            }
            if (method.MethodKind == MethodKind.EventAdd)
            {
                WriteDefaultEventAdder(method);
            }
            else if (method.MethodKind == MethodKind.EventRemove)
            {
                WriteDefaultEventRemover(method);
            }
            else
            {
                BeginBlock();
                EndBlock();
            }
        }

        private void WriteDefaultEventRemover(IMethodSymbol method)
        {
            BeginBlock();
            var eventSymbol = (IEventSymbol)method.AssociatedSymbol;
            Write(Emitter.GetEventName(eventSymbol));
            Write(" = ");

            WriteEventType((INamedTypeSymbol)method.Parameters[0].Type);
            Write("::Remove");
            WriteOpenParentheses();

            Write(Emitter.GetEventName(eventSymbol));
            Write(", ");
            Write(method.Parameters[0].Name);

            WriteCloseParentheses();
            WriteSemiColon();
            WriteNewLine();
            EndBlock();
        }

        private void WriteDefaultEventAdder(IMethodSymbol method)
        {
            BeginBlock();
            var eventSymbol = (IEventSymbol)method.AssociatedSymbol;

            Write(Emitter.GetEventName(eventSymbol));
            Write(" = ");

            WriteEventType((INamedTypeSymbol)method.Parameters[0].Type);
            Write("::Combine");
            WriteOpenParentheses();

            Write(Emitter.GetEventName(eventSymbol));
            Write(", ");
            Write(method.Parameters[0].Name);

            WriteCloseParentheses();
            WriteSemiColon();
            WriteNewLine();
            EndBlock();
        }

        private void WriteAutoPropertyGetter(IMethodSymbol method)
        {
            var property = (IPropertySymbol)method.AssociatedSymbol;
            var backingField = method.ContainingType
                .GetMembers()
                .OfType<IFieldSymbol>()
                .FirstOrDefault(f => Equals(f.AssociatedSymbol, property));
            Write("return ");
            Write(Emitter.GetFieldName(backingField));
            WriteSemiColon();
        }


        private void WriteAutoPropertySetter(IMethodSymbol method)
        {
            var property = (IPropertySymbol)method.AssociatedSymbol;
            var backingField = method.ContainingType
                .GetMembers()
                .OfType<IFieldSymbol>()
                .FirstOrDefault(f => Equals(f.AssociatedSymbol, property));

            Write("return ");
            Write(Emitter.GetFieldName(backingField));
            Write(" = ");
            Write(method.Parameters[0].Name);
            WriteSemiColon();
        }

        protected AutoCastMode WriteConstant(IFieldSymbol constField)
        {
            switch (constField.Type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    Write((bool)constField.ConstantValue ? "true" : "false");
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    Write((int)constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Decimal:
                    Write((decimal)constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Single:
                    Write((float)constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_Double:
                    Write((double)constField.ConstantValue);
                    return AutoCastMode.SkipCast;
                case SpecialType.System_String:
                    Write("\"" + constField.ConstantValue + "\"");
                    return AutoCastMode.SkipCast;
                default:
                    if (constField.Type.TypeKind == TypeKind.Enum)
                    {
                        WriteType(constField.Type);
                        Write("::");
                        Write(constField.Name);
                        return AutoCastMode.SkipCast;
                    }

                    Log.Error("Unknown constant type: " + constField.Type);
                    throw new PhaseCompilerException("Unknown constant type: " + constField.Type);
            }
        }

        protected void WriteDeclspec()
        {
            Write(EmitterContext.CurrentType.SemanticModel.Compilation.Assembly.Name.Replace(".dll", "").Replace(".", "").ToUpperInvariant() + "_API");
        }

        public void WriteWithAutoCast(AutoCastMode mode, ITypeSymbol convertedType, ITypeSymbol type, string result)
        {
            if (mode == AutoCastMode.SkipCast)
            {
                Write(result);
                return;
            }

            // implicit cast
            if (convertedType != null && type != null && !type.Equals(convertedType))
            {
                // boxing? 
                if (convertedType.SpecialType == SpecialType.System_Object)
                {
                    switch (type.SpecialType)
                    {
                        case SpecialType.System_String:
                            Write("System::String::Box(");
                            Write(result);
                            WriteCloseParentheses();
                            return;
                        case SpecialType.System_Boolean:
                        case SpecialType.System_Char:
                        case SpecialType.System_Byte:
                        case SpecialType.System_SByte:
                        case SpecialType.System_Int16:
                        case SpecialType.System_Int32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt16:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_UInt64:
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                            var extensionsName = Emitter.GetTypeName(type, false, true, CppEmitter.TypeNamePointerKind.NoPointer) + "Extensions";
                            Write(extensionsName, "::Box(");
                            Write(result);
                            WriteCloseParentheses();
                            return;
                    }
                }

                // int to single/double promotion
                switch (type.SpecialType)
                {
                    case SpecialType.System_SByte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                        switch (convertedType.SpecialType)
                        {
                            case SpecialType.System_Single:
                            case SpecialType.System_Double:
                                Write("static_cast<");
                                Write(Emitter.GetTypeName(convertedType, false, true, CppEmitter.TypeNamePointerKind.NoPointer));
                                Write(">(", result, ")");
                                return;
                        }
                        break;
                }

                // no implicit casts to IEnumerable
                if (type.IsReferenceType && convertedType.SpecialType == SpecialType.System_Collections_IEnumerable)
                {
                    Write(result);
                    return;
                }
                
                // cast to base class/interface?
                if(type.TypeKind != TypeKind.Array && type.IsReferenceType && convertedType.IsReferenceType)
                {
                    var typeName = Emitter.GetTypeName(convertedType, false, false, CppEmitter.TypeNamePointerKind.NoPointer);
                    Write("std::static_pointer_cast<", typeName, ">(");
                    Write(result);
                    WriteCloseParentheses();
                    return;
                }
            }

            Write(result);
        }
    }

    public abstract class AbstractCppEmitterBlock<T> : AbstractCppEmitterBlock where T : SyntaxNode
    {
        public T Node { get; set; }

        public virtual void Emit(CppEmitterContext context, T node, CancellationToken cancellationToken = default(CancellationToken))
        {
            EmitterContext = context;
            Node = node;
            Emit(cancellationToken);
        }

        protected override void BeginEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            base.BeginEmit(cancellationToken);
            EmitterContext.BeginEmit(Node);
        }

        protected override void EndEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            base.EndEmit(cancellationToken);
            EmitterContext.EndEmit(Node);
        }
    }
}
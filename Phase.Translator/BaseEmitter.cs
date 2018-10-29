using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using Phase.Attributes;
using Phase.Translator.Utils;

namespace Phase.Translator
{
    public abstract class BaseEmitter : IEmitter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        protected class SymbolMetaData
        {
            public string OutputName { get; set; }
            public bool? HasConstructorOverloads { get; set; }
            public int? ConstructorCount { get; set; }
            public bool? IsAutoProperty { get; set; }
            public bool? IsNullable { get; set; }
            public bool? IsReifiedExtensionMethod { get; set; }
            public bool? IsEventField { get; set; }
            public bool? NeedsDefaultInitializer { get; set; }
            public Optional<ForeachMode?> ForeachMode { get; set; }
            public Optional<string> Native { get; set; }
            public bool? IsRawParams { get; set; }
            public string Meta { get; set; }
        }

        private ConcurrentDictionary<ISymbol, SymbolMetaData> _symbolMetaCache;
        protected ConcurrentDictionary<string, List<ITypeSymbol>> TypeLookup { get; private set; }
        private ConcurrentDictionary<SyntaxTree, SemanticModel> _semanticModelLookup;

        public PhaseCompiler Compiler { get; set; }


        public BaseEmitter(PhaseCompiler compiler)
        {
            Compiler = compiler;
            _symbolMetaCache = new ConcurrentDictionary<ISymbol, SymbolMetaData>(SymbolEquivalenceComparer.Instance);
            _semanticModelLookup = new ConcurrentDictionary<SyntaxTree, SemanticModel>();
        }

        public abstract Task<EmitResult> EmitAsync(CSharpCompilation compilation, IEnumerable<PhaseType> types,
            CancellationToken cancellationToken);

        protected void BuildTypeNameCache(IEnumerable<PhaseType> types)
        {
            TypeLookup = new ConcurrentDictionary<string, List<ITypeSymbol>>();
            foreach (var type in types)
            {
                var x = GetNamespaceAndTypeName(type.TypeSymbol);
                var key = x.Item1 + "." + x.Item2;
                if (!TypeLookup.TryGetValue(key, out var typesForName))
                {
                    typesForName = TypeLookup[key] = new List<ITypeSymbol>();
                }

                foreach (var partialDeclaration in type.PartialDeclarations)
                {
                    _semanticModelLookup[partialDeclaration.RootNode.SyntaxTree] = partialDeclaration.SemanticModel;
                }
                typesForName.Add(type.TypeSymbol);
            }
        }

        public void Init()
        {
        }

        public string GetTypeName(PhaseType type)
        {
            return GetTypeName(type.TypeSymbol);
        }

        public string GetTypeName(TypeSyntax type, bool simple = false)
        {
            return GetTypeName(GetTypeSymbol(type), simple);
        }

        public bool IsPhaseClass(ITypeSymbol type)
        {
            return type.Equals(GetPhaseType("Phase.Script"));
        }

        public abstract string GetTypeName(ITypeSymbol type, bool simple = false, bool noTypeArguments = false);


        public ImmutableArray<ITypeSymbol> GetTypeArguments(INamedTypeSymbol named)
        {
            var current = named;
            while (current.TypeArguments.Length == 0 && current.ContainingType != null)
            {
                current = current.ContainingType;
            }
            return current.TypeArguments;
        }

        public string GetNameFromAttribute(ISymbol type)
        {
            var nameAttribute = GetAttributes(type).FirstOrDefault(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.NameAttribute")));
            if (nameAttribute != null)
            {
                return nameAttribute.ConstructorArguments[0].Value.ToString();
            }

            return null;
        }

        protected abstract Tuple<string, string> GetNamespaceAndTypeName(ITypeSymbol type);

        public IEnumerable<AttributeData> GetAttributes(ISymbol type)
        {
            return Compiler.Translator.Attributes.GetAttributes(type);
        }


        protected string SafeName(string name)
        {
            if (IsKeyWord(name))
            {
                return "_" + name;
            }

            return name;
        }

        protected virtual bool IsKeyWord(string name)
        {
            return false;
        }

        public bool IsExternal(ISymbol symbol)
        {
            if (GetAttributes(symbol)
                .Any(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.ExternalAttribute"))))
            {
                return true;
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Event:
                case SymbolKind.Field:
                case SymbolKind.Method:
                case SymbolKind.Property:
                    if (IsNative(symbol.ContainingType))
                    {
                        return false;
                    }
                    break;
            }
            return symbol.IsExtern;
        }

        public CodeTemplate GetTemplate(ISymbol symbol)
        {
            var attribute = GetAttributes(symbol).FirstOrDefault(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.TemplateAttribute")));
            if (attribute == null)
            {
                return null;
            }

            var skipSemicolonOnStatements = attribute.NamedArguments
                .Where(arg => arg.Key == "SkipSemicolonOnStatements")
                .Select(arg => (bool)arg.Value.Value).FirstOrDefault();
            return new CodeTemplate(attribute.ConstructorArguments[0].Value.ToString(), skipSemicolonOnStatements);
        }

        public virtual bool IsMethodRedirected(IMethodSymbol methodSymbol, out string typeName)
        {
            var attribute = GetAttributes(methodSymbol.ContainingType).FirstOrDefault(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.RedirectMethodsToAttribute")));
            if (attribute == null)
            {
                typeName = null;
                return false;
            }

            typeName = attribute.ConstructorArguments[0].Value.ToString();
            return true;
        }

        public string GetFieldName(IFieldSymbol field)
        {
            var attributeName = GetNameFromAttribute(field);
            if (!string.IsNullOrEmpty(attributeName))
            {
                return attributeName;
            }
            if (field.AssociatedSymbol is IPropertySymbol p)
            {
                return "__" + GetPropertyName(p);
            }
            return GetFieldNameInternal(field);
        }

        protected virtual string GetFieldNameInternal(IFieldSymbol field)
        {
            return field.Name;
        }

        protected SymbolMetaData GetOrCreateMeta(ISymbol symbol)
        {
            lock (this)
            {
                if (!_symbolMetaCache.TryGetValue(symbol, out var meta))
                {
                    _symbolMetaCache[symbol] = meta = new SymbolMetaData();
                }
                return meta;
            }
        }

        public string GetMethodName(IMethodSymbol method)
        {
            lock (method)
            {
                var meta = GetOrCreateMeta(method);
                if (meta.OutputName != null)
                {
                    return meta.OutputName;
                }

                return meta.OutputName = GetMethodNameInternal(method);
            }
        }

        public bool HasConstructorOverloads(ITypeSymbol type)
        {
            if (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct)
            {
                return false;
            }

            lock (this)
            {
                var meta = GetOrCreateMeta(type);
                if (meta.HasConstructorOverloads == null)
                {
                    ComputeConstructorOverloads(type, meta);
                }

                return meta.HasConstructorOverloads.Value;
            }
        }

        public int GetConstructorCount(ITypeSymbol type)
        {
            if (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct)
            {
                return 0;
            }

            lock (this)
            {
                var meta = GetOrCreateMeta(type);
                if (meta.ConstructorCount == null)
                {
                    ComputeConstructorOverloads(type, meta);
                }
                return meta.ConstructorCount.Value;
            }
        }

        private void ComputeConstructorOverloads(ITypeSymbol type, SymbolMetaData meta)
        {
            meta.ConstructorCount = type.GetMembers().Count(t =>
                t.Kind == SymbolKind.Method && ((IMethodSymbol)t).MethodKind == MethodKind.Constructor && !t.IsStatic);
            meta.HasConstructorOverloads = meta.ConstructorCount > 1;

            if (meta.ConstructorCount < 2 && type.BaseType != null && type.BaseType.SpecialType != SpecialType.System_Object)
            {
                meta.HasConstructorOverloads = HasConstructorOverloads(type.BaseType);
            }
        }

        protected abstract string GetMethodNameInternal(IMethodSymbol method);

        public virtual string GetSymbolName(ISymbol symbol, BaseEmitterContext context)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Alias:
                case SymbolKind.Assembly:
                case SymbolKind.Label:
                case SymbolKind.Local:
                case SymbolKind.NetModule:
                case SymbolKind.Namespace:
                case SymbolKind.Parameter:
                case SymbolKind.RangeVariable:
                case SymbolKind.Preprocessing:
                case SymbolKind.Discard:
                    return EscapeKeyword(symbol.Name);
                case SymbolKind.ArrayType:
                case SymbolKind.DynamicType:
                case SymbolKind.ErrorType:
                case SymbolKind.NamedType:
                case SymbolKind.PointerType:
                case SymbolKind.TypeParameter:
                    return GetTypeName((ITypeSymbol)symbol);
                case SymbolKind.Event:
                    return GetEventName((IEventSymbol)symbol);
                case SymbolKind.Field:
                    return GetFieldName((IFieldSymbol)symbol);
                case SymbolKind.Method:
                    return GetMethodName((IMethodSymbol)symbol);
                case SymbolKind.Property:
                    return GetPropertyName((IPropertySymbol)symbol);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual string EscapeKeyword(string symbolName)
        {
            return symbolName;
        }


        public string GetPropertyName(IPropertySymbol property)
        {
            var attributeName = GetNameFromAttribute(property);
            if (!string.IsNullOrEmpty(attributeName))
            {
                return attributeName;
            }

            if (!property.ExplicitInterfaceImplementations.IsEmpty)
            {
                var impl = property.ExplicitInterfaceImplementations[0];
                return GetTypeName(impl.ContainingType, true) + "_" + GetPropertyName(impl);
            }

            return GetPropertyNameInternal(property);
        }

        protected virtual string GetPropertyNameInternal(IPropertySymbol property)
        {
            return property.Name.ToCamelCase();
        }


        public virtual string GetEventName(IEventSymbol eventSymbol)
        {
            if (!eventSymbol.ExplicitInterfaceImplementations.IsEmpty)
            {
                var impl = eventSymbol.ExplicitInterfaceImplementations[0];
                return GetTypeName(impl.ContainingType, true) + "_" + GetEventName(impl);
            }

            return eventSymbol.Name.ToCamelCase();
        }

        public string GetDelegateName(INamedTypeSymbol delegateSymbol)
        {
            return delegateSymbol.Name;
        }

        public SymbolInfo GetSymbolInfo(OrderingSyntax node,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSemanticModel(node).GetSymbolInfo(node, cancellationToken);
        }

        private SemanticModel GetSemanticModel(SyntaxNode node)
        {
            if (!_semanticModelLookup.TryGetValue(node.SyntaxTree, out var model))
            {
                _semanticModelLookup[node.SyntaxTree] = model =
                    Compiler.Translator.Compilation.GetSemanticModel(node.SyntaxTree);
            }

            return model;
        }

        public SymbolInfo GetSymbolInfo(SelectOrGroupClauseSyntax node,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSemanticModel(node).GetSymbolInfo(node, cancellationToken);
        }

        public SymbolInfo GetSymbolInfo(ExpressionSyntax expression,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSemanticModel(expression).GetSymbolInfo(expression, cancellationToken);
        }

        public SymbolInfo GetSymbolInfo(ConstructorInitializerSyntax constructorInitializer,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSemanticModel(constructorInitializer).GetSymbolInfo(constructorInitializer, cancellationToken);
        }

        public SymbolInfo GetSymbolInfo(AttributeSyntax attributeSyntax,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSemanticModel(attributeSyntax).GetSymbolInfo(attributeSyntax, cancellationToken);
        }

        public SymbolInfo GetSymbolInfo(CrefSyntax crefSyntax,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSemanticModel(crefSyntax).GetSymbolInfo(crefSyntax, cancellationToken);
        }

        public ISymbol GetDeclaredSymbol(TypeSyntax syntax,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSemanticModel(syntax).GetDeclaredSymbol(syntax, cancellationToken);
        }


        public ISymbol GetDeclaredSymbol(ForEachStatementSyntax syntax,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSemanticModel(syntax).GetDeclaredSymbol(syntax, cancellationToken);
        }

        public ISymbol GetDeclaredSymbol(VariableDeclaratorSyntax syntax,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSemanticModel(syntax).GetDeclaredSymbol(syntax, cancellationToken);
        }

        public ISymbol GetDeclaredSymbol(VariableDeclarationSyntax syntax,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSemanticModel(syntax).GetDeclaredSymbol(syntax, cancellationToken);
        }

        public IParameterSymbol GetDeclaredSymbol(ParameterSyntax syntax,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSemanticModel(syntax).GetDeclaredSymbol(syntax, cancellationToken);
        }

        public TypeInfo GetTypeInfo(SyntaxNode syntax,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSemanticModel(syntax).GetTypeInfo(syntax, cancellationToken);
        }



        public Optional<object> GetConstantValue(SyntaxNode node,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSemanticModel(node).GetConstantValue(node, cancellationToken);
        }

        private Dictionary<SpecialType, string> _specialArrayTypes;

        public string GetSpecialArrayName(ITypeSymbol elementType, bool simple = false)
        {
            if (_specialArrayTypes == null)
            {
                _specialArrayTypes = BuildSpecialArrayLookup();
            }

            if (_specialArrayTypes.TryGetValue(elementType.SpecialType, out var arrayType))
            {
                if (simple)
                {
                    return arrayType.Substring(arrayType.LastIndexOf(".") + 1);
                }

                return arrayType;
            }

            return null;
        }

        protected abstract Dictionary<SpecialType, string> BuildSpecialArrayLookup();

        public ITypeSymbol GetTypeSymbol(TypeSyntax syntax)
        {
            var type = GetTypeInfo(syntax).Type;
            if (type == null)
            {
                type = GetSymbolInfo(syntax).Symbol as ITypeSymbol;
            }
            if (type == null)
            {
                type = GetDeclaredSymbol(syntax) as ITypeSymbol;
            }
            if (type == null)
            {
                type = Compiler.Translator.Compilation.GetTypeByMetadataName(syntax.ToString());
            }
            if (type == null)
            {
                throw new PhaseCompilerException($"Could not resolve type from node {syntax}");
            }
            return type;
        }

        public virtual string GetDefaultValue(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Enum:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                    return "0";
                case SpecialType.System_Boolean:
                    return "false";
                case SpecialType.System_Char:
                    return "0";
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                    return "0.0";
                default:
                    if (type.TypeKind == TypeKind.Enum)
                    {
                        var field = GetDefaultEnumField(type);
                        if (field != null)
                        {
                            return field.Name;
                        }
                    }
                    return "null";
            }
        }

        protected IFieldSymbol GetDefaultEnumField(ITypeSymbol type)
        {
            return type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(f => (int)f.ConstantValue == 0) ??
                   type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault();
        }

        public bool IsRefVariable(VariableDeclaratorSyntax variable)
        {
            return false;
            // TODO: support for ref
            SyntaxNode node = variable;
            BlockSyntax scope;
            do
            {
                scope = (node = node.Parent) as BlockSyntax;
            } while (scope == null);

            var symbol = GetDeclaredSymbol(variable);
            return scope.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .SelectMany(o => o.ArgumentList.Arguments)
                .Where(o => o.RefOrOutKeyword.Kind() != SyntaxKind.None)
                .Any(o => GetSymbolInfo(o.Expression).Symbol.Equals(symbol));
        }

        public bool IsRefVariable(ILocalSymbol symbol)
        {
            return false;
            // TODO: support for ref

            var variable = symbol.DeclaringSyntaxReferences.Select(f => f.GetSyntax()).First(f => f != null);
            SyntaxNode node = variable;
            BlockSyntax scope;
            do
            {
                scope = (node = node.Parent) as BlockSyntax;
            } while (scope == null);

            return scope.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .SelectMany(o => o.ArgumentList.Arguments)
                .Where(o => o.RefOrOutKeyword.Kind() != SyntaxKind.None)
                .Any(o => GetSymbolInfo(o.Expression).Symbol.Equals(symbol));
        }

        public AttributeData GetAbstract(INamedTypeSymbol type)
        {
            return GetAttributes(type).FirstOrDefault(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.AbstractAttribute")));
        }

        public bool IsAbstract(INamedTypeSymbol type)
        {
            return GetAttributes(type).Any(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.AbstractAttribute")));
        }

        public bool IsNativeIndexer(ISymbol symbol)
        {
            return GetAttributes(symbol).Any(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.NativeIndexerAttribute")));
        }

        private ConcurrentDictionary<string, INamedTypeSymbol> _typeCache = new ConcurrentDictionary<string, INamedTypeSymbol>();

        public INamedTypeSymbol GetPhaseType(string name)
        {
            if (_typeCache.TryGetValue(name, out var type))
            {
                return type;
            }
            type = Compiler.Translator.Compilation.GetTypeByMetadataName(name);
            if (type == null)
            {
                // TODO look in mscorlib
            }
            return _typeCache[name] = type;
        }

        public bool IsInline(IMethodSymbol method)
        {
            return GetAttributes(method).Any(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.InlineAttribute")));
        }

        public bool IsCompilerExtension(IMethodSymbol method)
        {

            return GetAttributes(method).Any(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.CompilerExtensionAttribute")));
        }

        public bool IsFrom(IMethodSymbol method)
        {
            return GetAttributes(method).Any(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.FromAttribute")));
        }

        public bool IsTo(IMethodSymbol method)
        {
            return GetAttributes(method).Any(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.ToAttribute")));
        }

        public string GetOp(IMethodSymbol method)
        {
            var attribute = GetAttributes(method).FirstOrDefault(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.OpAttribute")));
            if (attribute == null)
            {
                return null;
            }

            return (string)attribute.ConstructorArguments[0].Value;
        }

        public bool HasNativeConstructors(ISymbol symbol)
        {
            return GetAttributes(symbol).Any(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.NativeConstructorsAttribute")));
        }

        public bool HasNoConstructor(ISymbol symbol)
        {
            return GetAttributes(symbol).Any(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.NoConstructorAttribute")));
        }

        public ITypeSymbol GetSpecialType(SpecialType specialType)
        {
            return Compiler.Translator.Compilation.GetSpecialType(specialType);
        }

        public bool NeedsDefaultInitializer(IPropertySymbol property)
        {
            lock (this)
            {
                var meta = GetOrCreateMeta(property);

                if (meta.NeedsDefaultInitializer.HasValue)
                {
                    return meta.NeedsDefaultInitializer.Value;
                }

                return (meta.NeedsDefaultInitializer = InternalNeedsDefaultInitializer(property)).Value;
            }
        }

        public bool IsEventField(IEventSymbol evt)
        {
            lock (this)
            {
                var meta = GetOrCreateMeta(evt);

                if (meta.IsEventField.HasValue)
                {
                    return meta.IsEventField.Value;
                }

                return (meta.IsEventField = InternalIsEventField(evt)).Value;
            }
        }


        public bool IsNullable(ISymbol symbol)
        {
            var meta = GetOrCreateMeta(symbol);
            if (meta.IsNullable.HasValue)
            {
                return meta.IsNullable.Value;
            }

            return (meta.IsNullable = IsNullableInternal(symbol)).Value;
        }



        public bool IsReifiedExtensionMethod(IMethodSymbol symbol)
        {
            var meta = GetOrCreateMeta(symbol);
            if (meta.IsNullable.HasValue)
            {
                return meta.IsReifiedExtensionMethod.Value;
            }

            return (meta.IsReifiedExtensionMethod = IsReifiedExtensionMethodInternal(symbol)).Value;
        }


        public ForeachMode? GetForeachMode(ITypeSymbol type)
        {
            lock (this)
            {
                var meta = GetOrCreateMeta(type);

                if (meta.ForeachMode.HasValue)
                {
                    return meta.ForeachMode.Value;
                }

                return (meta.ForeachMode = InternalGetForeachMode(type)).Value;
            }
        }

        public bool IsNative(ISymbol symbol)
        {
            lock (this)
            {
                return !string.IsNullOrEmpty(GetNative(symbol));
            }
        }

        public string GetNative(ISymbol symbol)
        {
            lock (this)
            {
                var meta = GetOrCreateMeta(symbol);

                if (meta.Native.HasValue)
                {
                    return meta.Native.Value;
                }

                return (meta.Native = InternalGetNative(symbol)).Value;
            }
        }

        public bool IsAutoProperty(IPropertySymbol property)
        {
            lock (this)
            {
                var meta = GetOrCreateMeta(property);

                if (meta.IsAutoProperty.HasValue)
                {
                    return meta.IsAutoProperty.Value;
                }

                return (meta.IsAutoProperty = InternalIsAutoProperty(property)).Value;
            }
        }


        public bool IsRawParams(IMethodSymbol methodSymbol)
        {
            lock (this)
            {
                var meta = GetOrCreateMeta(methodSymbol);

                if (meta.IsRawParams.HasValue)
                {
                    return meta.IsRawParams.Value;
                }

                return (meta.IsRawParams = InternalIsRawParams(methodSymbol)).Value;
            }
        }

        private bool? InternalIsRawParams(IMethodSymbol methodSymbol)
        {
            var attr = methodSymbol.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.RawParamsAttribute")));
            if (attr == null)
            {
                return false;
            }
            return true;
        }

        private bool InternalIsAutoProperty(IPropertySymbol property)
        {
            var attr = GetAttributes(property).FirstOrDefault(a =>
                a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.AutoPropertyAttribute")));
            if (attr != null)
            {
                return true;
            }

            if (property.ContainingType.TypeKind != TypeKind.Class && property.ContainingType.TypeKind != TypeKind.Struct)
            {
                return false;
            }

            if (property.IsAbstract || property.IsExtern)
            {
                return false;
            }

            if (IsInterfaceImplementation(property, out var interfaceMember))
            {
                return IsAutoProperty((IPropertySymbol)interfaceMember);
            }

            var declaration = (BasePropertyDeclarationSyntax)property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (declaration != null)
            {
                return IsAutoProperty(declaration);
            }
            return false;
        }

        private bool IsNullableInternal(ISymbol symbol)
        {
            return GetAttributes(symbol)
                .Any(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.NullableAttribute")));
        }

        private bool IsReifiedExtensionMethodInternal(ISymbol symbol)
        {
            return GetAttributes(symbol)
                .Any(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.ReifiedExtensionMethodAttribute")));
        }

        private bool InternalNeedsDefaultInitializer(IPropertySymbol property)
        {
            var attr = property.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.AutoPropertyAttribute")));
            if (attr != null)
            {
                return true;
            }

            if (property.ContainingType.TypeKind != TypeKind.Class && property.ContainingType.TypeKind != TypeKind.Struct)
            {
                return false;
            }

            if (property.IsAbstract || property.IsExtern)
            {
                return false;
            }

            foreach (var fieldReference in property.DeclaringSyntaxReferences)
            {
                if (fieldReference.GetSyntax() is PropertyDeclarationSyntax declaration)
                {
                    if (declaration.Initializer != null)
                    {
                        return false;
                    }

                    if (IsAutoProperty(declaration))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
        private bool InternalIsEventField(IEventSymbol evt)
        {
            if (evt.ContainingType.TypeKind != TypeKind.Class && evt.ContainingType.TypeKind != TypeKind.Struct)
            {
                return false;
            }

            if (evt.IsAbstract || evt.IsExtern)
            {
                return false;
            }

            if (IsInterfaceImplementation(evt))
            {
                return false;
            }

            var declaration = evt.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (declaration.Kind() == SyntaxKind.EventFieldDeclaration || declaration.Kind() == SyntaxKind.VariableDeclarator)
            {
                return true;
            }
            return false;
        }

        private Optional<ForeachMode?> InternalGetForeachMode(ITypeSymbol type)
        {
            var attr = type.GetAttributes().FirstOrDefault(t => t.AttributeClass.Equals(GetPhaseType("Phase.Attributes.ForeachModeAttribute")));
            if (attr == null)
            {
                if (type.TypeKind == TypeKind.Array)
                {
                    return ForeachMode.Native;
                }

                return new Optional<ForeachMode?>(null);
            }

            return (ForeachMode)(int)attr.ConstructorArguments[0].Value;
        }



        private Optional<string> InternalGetNative(ISymbol symbol)
        {
            var attr = symbol.GetAttributes().FirstOrDefault(t => t.AttributeClass.Equals(GetPhaseType("Phase.Attributes.NativeAttribute")));
            if (attr == null)
            {
                return new Optional<string>(null);
            }
            return (string)attr.ConstructorArguments[0].Value;
        }

        public CastMode GetCastMode(ITypeSymbol type)
        {
            var attr = type.GetAttributes().FirstOrDefault(t => t.AttributeClass.Equals(GetPhaseType("Phase.Attributes.CastModeAttribute")));
            if (attr == null)
            {
                return CastMode.SafeCast;
            }

            return (CastMode)(int)attr.ConstructorArguments[0].Value;
        }

        public bool IsInterfaceImplementation(ISymbol method)
        {
            return method.ContainingType.AllInterfaces.SelectMany(@interface => @interface.GetMembers()).Any(interfaceMethod => method.ContainingType.FindImplementationForInterfaceMember(interfaceMethod)?.Equals(method) ?? false);
        }

        private bool IsInterfaceImplementation(ISymbol method, out ISymbol interfaceMember)
        {
            interfaceMember = method.ContainingType.AllInterfaces.SelectMany(@interface => @interface.GetMembers()).FirstOrDefault(interfaceMethod => method.ContainingType.FindImplementationForInterfaceMember(interfaceMethod).Equals(method));
            return interfaceMember != null;
        }

        private bool IsAutoProperty(BasePropertyDeclarationSyntax declaration)
        {
            AccessorDeclarationSyntax getSyntax = null;
            AccessorDeclarationSyntax setSyntax = null;
            if (declaration.AccessorList != null)
            {
                foreach (var accessor in declaration.AccessorList.Accessors)
                {
                    switch (accessor.Kind())
                    {
                        case SyntaxKind.GetAccessorDeclaration:
                            getSyntax = accessor;
                            break;
                    }
                }
            }

            if (getSyntax != null)
            {
                return getSyntax.Body == null && getSyntax.ExpressionBody == null;
            }

            return false;
        }

        public bool IsIConvertible(ITypeSymbol type)
        {
            return type.AllInterfaces.Any(i => i.Equals(GetPhaseType("System.IConvertible")));
        }

        public bool IsGetEnumeratorAsIterator(IMethodSymbol method)
        {
            if (method.Name != "GetEnumerator")
            {
                return false;
            }

            var enumerable = method.ContainingType.AllInterfaces.FirstOrDefault(i =>
                i.OriginalDefinition.Equals(GetPhaseType("System.Collections.Generic.IEnumerable`1")));
            if (enumerable == null)
            {
                return false;
            }
            var getEnumerator = enumerable.GetMembers("GetEnumerator")[0];
            var interfaceMember = method.ContainingType.FindImplementationForInterfaceMember(getEnumerator);
            if (interfaceMember != null && interfaceMember.Equals(method))
            {
                var foreachMode = GetForeachMode(method.ContainingType);
                return foreachMode == ForeachMode.GetEnumerator;
            }
            return false;
        }
        
        public bool NeedsDefaultInitializer(IPropertySymbol property)
        {
            lock (this)
            {
                var meta = GetOrCreateMeta(property);

                if (meta.NeedsDefaultInitializer.HasValue)
                {
                    return meta.NeedsDefaultInitializer.Value;
                }

                return (meta.NeedsDefaultInitializer = InternalNeedsDefaultInitializer(property)).Value;
            }

        }

        private bool InternalNeedsDefaultInitializer(IPropertySymbol property)
        {
            var attr = property.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.AutoPropertyAttribute")));
            if (attr != null)
            {
                return true;
            }

            if (property.ContainingType.TypeKind != TypeKind.Class && property.ContainingType.TypeKind != TypeKind.Struct)
            {
                return false;
            }

            if (property.IsAbstract || property.IsExtern)
            {
                return false;
            }

            var declaration = (BasePropertyDeclarationSyntax)property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (declaration != null)
            {
                return IsAutoProperty(declaration);
            }
            return false;
        }


        public string GetMeta(ISymbol symbol)
        {
            lock (this)
            {
                var meta = GetOrCreateMeta(symbol);
                if (meta.Meta != null)
                {
                    return meta.Meta;
                }
                return meta.Meta = GetMetaInternal(symbol);
            }
        }

        private string GetMetaInternal(ISymbol symbol)
        {
            var attr = symbol.GetAttributes()
                .FirstOrDefault(s => s.AttributeClass.Equals(GetPhaseType("Phase.Attributes.MetaAttribute")));
            if (attr == null)
            {
                return string.Empty;
            }
            return (string)attr.ConstructorArguments[0].Value;
        }

        public bool TryGetCallerMemberInfo(IParameterSymbol parameter, ISymbol callerMember, SyntaxNode callerNode, out string value)
        {
            var callerAttribute = parameter.GetAttributes().FirstOrDefault(
                a => a.AttributeClass.Equals(GetPhaseType("System.Runtime.CompilerServices.CallerMemberNameAttribute"))
                    || a.AttributeClass.Equals(GetPhaseType("System.Runtime.CompilerServices.CallerLineNumberAttribute"))
                    || a.AttributeClass.Equals(GetPhaseType("System.Runtime.CompilerServices.CallerFilePathAttribute"))
            );
            if (callerAttribute == null)
            {
                value = null;
                return false;
            }
            switch (callerAttribute.AttributeClass.Name)
            {
                case "CallerMemberNameAttribute":
                    if (callerMember == null)
                    {
                        value = null;
                        Log.Warn("Could not get caller member name");
                        return false;
                    }
                    value = "\"" + GetSymbolName(callerMember) + "\"";
                    return true;
                case "CallerLineNumberAttribute":
                    if (callerNode == null)
                    {
                        value = null;
                        Log.Warn("Could not get caller line number");
                        return false;
                    }
                    value = callerNode.GetText().Lines[0].LineNumber.ToString();
                    return true;
                case "CallerFilePathAttribute":
                    if (callerNode == null)
                    {
                        value = null;
                        Log.Warn("Could not get caller file path");
                        return false;
                    }
                    value = "\"" + callerNode.SyntaxTree.FilePath.Replace("\\", "\\\\") + "\"";
                    return true;
            }

            value = null;
            return false;
        }
    }

    public abstract class BaseEmitter<TContext> : BaseEmitter
        where TContext : BaseEmitterContext
    {
        private readonly Logger _log;

        protected BaseEmitter(PhaseCompiler compiler, Logger log) : base(compiler)
        {
            _log = log;
        }

        protected abstract IEnumerable<TContext> BuildEmitterContexts(PhaseType type);

        public override async Task<EmitResult> EmitAsync(CSharpCompilation compilation, IEnumerable<PhaseType> types, CancellationToken cancellationToken)
        {
            var result = new EmitResult();

            var typeArray = types.ToArray();

            BuildTypeNameCache(typeArray);

            var emitBlock = new ActionBlock<BaseEmitterContext>(
                context =>
                {
                    try
                    {
                        context.Emit(cancellationToken);
                    }
                    catch (Exception e)
                    {
                        var location = context.LastKnownNode?.GetLocation();
                        _log.Error(CultureInfo.InvariantCulture, Diagnostic.Create(PhaseErrors.PH017, location, e.ToString()));
                        throw;
                    }
                }, new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Compiler.Options.ProcessorCount
                });

            var contexts = typeArray.Where(t => !IsExternal(t.TypeSymbol)).SelectMany(BuildEmitterContexts).ToArray();

            foreach (var type in contexts)
            {
                emitBlock.Post(type);
            }
            emitBlock.Complete();
            await emitBlock.Completion;

            foreach (var context in contexts)
            {
                if (!result.Results.TryGetValue(context.CurrentType, out var results))
                {
                    result.Results[context.CurrentType] = results = new List<PhaseTypeResult>();
                }

                results.Add(new PhaseTypeResult(context.FileName, context.Writer.ToString()));
            }

            return result;
        }
    }


}
﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Haxe;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using NLog.Fluent;
using Phase.Attributes;
using Phase.Translator.Haxe.Expressions;
using Phase.Translator.Utils;

namespace Phase.Translator.Haxe
{
    public class HaxeEmitter : IEmitter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private class SymbolMetaData
        {
            public string OutputName { get; set; }
            public bool? HasConstructorOverloads { get; set; }
            public int? ConstructorCount { get; set; }
            public bool? IsAutoProperty { get; set; }
            public bool? IsEventField { get; set; }
            public bool? NeedsDefaultInitializer { get; set; }
            public Optional<ForeachMode?> ForeachMode { get; set; }
            public Optional<string> Native { get; set; }
            public bool? IsRawParams { get; set; }
            public string Meta { get; set; }
        }

        private ConcurrentDictionary<ISymbol, SymbolMetaData> _symbolMetaCache;
        private ConcurrentDictionary<string, List<ITypeSymbol>> _typeLookup;
        private ConcurrentDictionary<SyntaxTree, SemanticModel> _semanticModelLookup;
        private ConcurrentDictionary<string, IMethodSymbol> _reservedMethodNames;


        public PhaseCompiler Compiler { get; set; }


        public HaxeEmitter(PhaseCompiler compiler)
        {
            Compiler = compiler;
            _symbolMetaCache = new ConcurrentDictionary<ISymbol, SymbolMetaData>(SymbolEquivalenceComparer.Instance);
            _reservedMethodNames = new ConcurrentDictionary<string, IMethodSymbol>();
            _semanticModelLookup = new ConcurrentDictionary<SyntaxTree, SemanticModel>();
        }

        public async Task<EmitResult> EmitAsync(CSharpCompilation compilation, IEnumerable<PhaseType> types, CancellationToken cancellationToken)
        {
            var result = new EmitResult();

            var typeArray = types.ToArray();

            BuildTypeNameCache(typeArray);

            var emitBlock = new ActionBlock<HaxeEmitterContext>(
                async context =>
                {
                    await context.EmitAsync(cancellationToken);
                }, new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Compiler.Options.ProcessorCount
                });

            var contexts = typeArray.Where(t => !IsExternal(t.TypeSymbol)).Select(t => new HaxeEmitterContext(this, t)).ToArray();
            foreach (var type in contexts)
            {
                emitBlock.Post(type);
            }
            emitBlock.Complete();
            await emitBlock.Completion;

            foreach (var context in contexts)
            {
                result.Results[context.CurrentType] = new PhaseTypeResult(GetFileName(context.CurrentType), context.Writer.ToString());
            }

            return result;
        }

        private void BuildTypeNameCache(IEnumerable<PhaseType> types)
        {
            _typeLookup = new ConcurrentDictionary<string, List<ITypeSymbol>>();
            foreach (var type in types)
            {
                var x = GetNamespaceAndTypeName(type.TypeSymbol);
                var key = x.Item1 + "." + x.Item2;
                List<ITypeSymbol> typesForName;
                if (!_typeLookup.TryGetValue(key, out typesForName))
                {
                    typesForName = _typeLookup[key] = new List<ITypeSymbol>();
                }

                foreach (var partialDeclaration in type.PartialDeclarations)
                {
                    _semanticModelLookup[partialDeclaration.RootNode.SyntaxTree] = partialDeclaration.SemanticModel;
                }
                typesForName.Add(type.TypeSymbol);
            }
        }

        private string GetFileName(PhaseType type)
        {
            var name = GetTypeName(type.TypeSymbol);
            var p = name.IndexOf("<");
            if (p >= 0) name = name.Substring(0, p);
            return name.Replace('.', Path.DirectorySeparatorChar) + ".hx";
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

        public string GetTypeName(ITypeSymbol type, bool simple = false, bool noTypeArguments = false)
        {
            var array = type as IArrayTypeSymbol;
            if (array != null)
            {
                var specialArray = GetSpecialArrayName(array.ElementType, simple);
                if (specialArray != null)
                {
                    return specialArray;
                }
                else
                {
                    return simple ? GetTypeName(array.ElementType, true) + "Array" : "system.FixedArray<" + GetTypeName(array.ElementType) + ">";
                }
            }

            if (type is IDynamicTypeSymbol)
            {
                return "Dynamic";
            }

            if (type is ITypeParameterSymbol)
            {
                return type.Name;
            }

            if (type.SpecialType == SpecialType.System_Void)
            {
                return "Void";
            }

            var nsAndName = GetNamespaceAndTypeName(type);
            var fullName = nsAndName.Item1 + "." + nsAndName.Item2;
            var name = simple || string.IsNullOrEmpty(nsAndName.Item1) ? nsAndName.Item2 : fullName;

            var named = type as INamedTypeSymbol;
            if (named != null && named.IsGenericType)
            {
                var typeArgs = GetTypeArguments(named);

                if (name == "system.Action" || name == "system.Func" || (_typeLookup.ContainsKey(fullName) && _typeLookup[fullName].Count > 1))
                {
                    name += typeArgs.Length;
                }

                if (!noTypeArguments && !simple)
                {
                    name += "<";

                    for (int i = 0; i < typeArgs.Length; i++)
                    {
                        if (i > 0) name += ", ";
                        name += GetTypeName(typeArgs[i]);
                    }
                    name += ">";
                }
            }

            var j = name.IndexOf("<");
            if (noTypeArguments && j != -1)
            {
                name = name.Substring(0, j);
            }

            return name;
        }


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

        private Tuple<string, string> GetNamespaceAndTypeName(ITypeSymbol type)
        {
            var nameAttribute = GetAttributes(type).FirstOrDefault(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.NameAttribute")));
            if (nameAttribute != null)
            {
                var name = nameAttribute.ConstructorArguments[0].Value.ToString();
                var keepNamespace = nameAttribute.ConstructorArguments.Length > 1 ? (bool)nameAttribute.ConstructorArguments[1].Value : false;
                var pkgEnd = name.LastIndexOf(".", StringComparison.OrdinalIgnoreCase);
                if (pkgEnd == -1)
                {
                    return Tuple.Create(keepNamespace ? GetNamespace(type) : "", name);
                }
                else
                {
                    return Tuple.Create(name.Substring(0, pkgEnd), name.Substring(pkgEnd + 1));
                }
            }

            return Tuple.Create(GetNamespace(type), BuildTypeName(type));
        }

        public IEnumerable<AttributeData> GetAttributes(ISymbol type)
        {
            return Compiler.Translator.Attributes.GetAttributes(type);
        }

        private string BuildTypeName(ITypeSymbol type)
        {
            var s = type.Name;
            while (type.ContainingType != null)
            {
                s = type.ContainingType.Name + "_" + s;
                type = type.ContainingType;
            }
            return s;
        }

        private string GetNamespace(ITypeSymbol type)
        {
            var ns = type.ContainingNamespace;
            var nss = new List<string>();
            while (ns != null && !ns.IsGlobalNamespace)
            {
                nss.Add(SafeName(ns.Name.ToCamelCase()));
                ns = ns.ContainingNamespace;
            }
            nss.Reverse();
            return string.Join(".", nss);
        }

        private string SafeName(string name)
        {
            if (IsKeyWord(name))
            {
                return "_" + name;
            }

            return name;
        }

        private bool IsKeyWord(string name)
        {
            switch (name)
            {
                case "dynamic":
                    return true;
            }
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

        public bool IsMethodRedirected(IMethodSymbol methodSymbol, out string typeName)
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
            if (field.AssociatedSymbol is IPropertySymbol)
            {
                return "__" + field.AssociatedSymbol.Name;
            }
            return field.Name;
        }

        private SymbolMetaData GetOrCreateMeta(ISymbol symbol)
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

        public string GetHaxeMeta(ISymbol symbol)
        {
            lock (this)
            {
                var meta = GetOrCreateMeta(symbol);
                if (meta.Meta != null)
                {
                    return meta.Meta;
                }
                return meta.Meta = GetHaxeMetaInternal(symbol);
            }
        }

        private string GetHaxeMetaInternal(ISymbol symbol)
        {
            var attr = GetAttributes(symbol)
                .FirstOrDefault(s => s.AttributeClass.Equals(GetPhaseType("Phase.Attributes.MetaAttribute")));
            if (attr == null)
            {
                return string.Empty;
            }
            return (string)attr.ConstructorArguments[0].Value;
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

        private string GetMethodNameInternal(IMethodSymbol method)
        {
            method = method.OriginalDefinition;

            if (method.MethodKind == MethodKind.StaticConstructor)
            {
                return "__init__";
            }

            if (!method.ExplicitInterfaceImplementations.IsEmpty)
            {
                var impl = method.ExplicitInterfaceImplementations[0];
                return GetTypeName(impl.ContainingType, true) + "_" + GetMethodName(impl);
            }

            var attributeName = GetNameFromAttribute(method);
            if (!string.IsNullOrEmpty(attributeName))
            {
                return attributeName;
            }

            if (method.OverriddenMethod != null)
            {
                return GetMethodName(method.OverriddenMethod);
            }

            var x = new StringBuilder();
            if (method.MethodKind == MethodKind.Constructor)
            {
                if (HasNativeConstructors(method.ContainingType) || !HasConstructorOverloads(method.ContainingType))
                {
                    return "new";
                }
                else
                {
                    x.Append(GetTypeName(method.ContainingType, true));
                }
            }
            else if (method.MethodKind == MethodKind.PropertyGet)
            {
                var prop = (IPropertySymbol) method.AssociatedSymbol;
                if (prop.IsIndexer)
                {
                    x.Append("get");
                }
                else
                {
                    x.Append("get_" + GetSymbolName(method.AssociatedSymbol));
                }
            }
            else if (method.MethodKind == MethodKind.PropertySet)
            {
                var prop = (IPropertySymbol) method.AssociatedSymbol;
                if (prop.IsIndexer)
                {
                    x.Append("set");
                }
                else
                {
                    x.Append("set_" + GetSymbolName(method.AssociatedSymbol));
                }
            }
            else if (method.MethodKind == MethodKind.EventAdd)
            {
                x.Append("add" + GetSymbolName(method.AssociatedSymbol).ToPascalCase());
            }
            else if (method.MethodKind == MethodKind.EventRemove)
            {
                x.Append("remove" + GetSymbolName(method.AssociatedSymbol).ToPascalCase());
            }
            else
            {
                x.Append(method.Name);
            }

            var overloads = method.ContainingType.GetMembers(method.Name).OfType<IMethodSymbol>().ToList();
            if (overloads.Count > 1)
            {
                foreach (IParameterSymbol p in method.Parameters)
                {
                    x.Append("_");

                    var name = GetTypeName(p.Type, true);
                    if (!string.IsNullOrEmpty(name))
                    {
                        x.Append(name);
                    }
                    else
                    {
                        x.Append(p.Type.Name);
                    }

                    var named = p.Type as INamedTypeSymbol;
                    if (named != null && named.IsGenericType)
                    {
                        for (int i = 0; i < named.TypeArguments.Length; i++)
                        {
                            x.Append("_");
                            name = GetTypeName(named.TypeArguments[i], true);
                            if (!string.IsNullOrEmpty(name))
                            {
                                x.Append(name);
                            }
                            else
                            {
                                x.Append(p.Type.Name);
                            }
                        }
                    }
                }

                if (method.TypeParameters.Length > 0)
                {
                    x.Append(method.TypeParameters.Length);
                }
            }

            var originalMethodName = x.ToString();
            var typeName = GetTypeName(method.ContainingType);
            var methodName = originalMethodName;
            var suffix = 1;
            while (_reservedMethodNames.ContainsKey(typeName + "." + methodName))
            {
                methodName = originalMethodName + suffix;
                suffix++;
            }
            _reservedMethodNames[typeName + "." + methodName] = method;

            if (method.MethodKind != MethodKind.Constructor)
            {
                methodName = methodName.ToCamelCase();
            }
            return methodName;
        }


        public string GetSymbolName(ISymbol symbol)
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
                    return symbol.Name;
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

            return property.Name.ToCamelCase();
        }


        public string GetEventName(IEventSymbol eventSymbol)
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


        public ISymbol GetDeclaredSymbol(VariableDeclaratorSyntax syntax,
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
                _specialArrayTypes = new Dictionary<SpecialType, string>
                {
                    [SpecialType.System_SByte] = "system.SByteArray",
                    [SpecialType.System_Byte] = ("system.ByteArray"),
                    [SpecialType.System_Int16] = ("system.Int16Array"),
                    [SpecialType.System_UInt16] = ("system.UInt16Array"),
                    [SpecialType.System_Int32] = ("system.Int32Array"),
                    [SpecialType.System_UInt32] = ("system.UInt32Array"),
                    [SpecialType.System_Int64] = ("system.Int64Array"),
                    [SpecialType.System_UInt64] = ("system.UInt64Array"),
                    [SpecialType.System_Decimal] = ("system.DecimalArray"),
                    [SpecialType.System_Single] = ("system.SingleArray"),
                    [SpecialType.System_Double] = ("system.DoubleArray")
                };
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

        public string GetDefaultValue(ITypeSymbol type)
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

        private IFieldSymbol GetDefaultEnumField(ITypeSymbol type)
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


        private bool InternalIsAutoProperty(IPropertySymbol property)
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

        private bool InternalIsEventField(IEventSymbol evt)
        {
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
            return method.ContainingType.AllInterfaces.SelectMany(@interface => @interface.GetMembers()).Any(interfaceMethod => method.ContainingType.FindImplementationForInterfaceMember(interfaceMethod).Equals(method));
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
}
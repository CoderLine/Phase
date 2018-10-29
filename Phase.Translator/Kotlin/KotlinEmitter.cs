using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using NLog;
using Phase.Translator.Utils;

namespace Phase.Translator.Kotlin
{
    public class KotlinEmitter : BaseEmitter<KotlinEmitterContext>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public ConcurrentDictionary<ITypeSymbol, PhaseType> NestedTypes { get; }

        public KotlinEmitter(PhaseCompiler compiler)
            : base(compiler, Log)
        {
            NestedTypes = new ConcurrentDictionary<ITypeSymbol, PhaseType>();
        }

        protected override IEnumerable<KotlinEmitterContext> BuildEmitterContexts(PhaseType type)
        {
            if (type.IsNested)
            {
                NestedTypes[type.TypeSymbol] = type;
                return Enumerable.Empty<KotlinEmitterContext>();
            }
            return new[] { new KotlinEmitterContext(this, type) };
        }

        public override string GetTypeName(ITypeSymbol type, bool simple = false, bool noTypeArguments = false)
        {
            return GetTypeName(type, simple, noTypeArguments, true);
        }

        public string GetTypeName(ITypeSymbol type, bool simple, bool noTypeArguments, bool nullable)
        {
            if (type is IArrayTypeSymbol array)
            {
                var specialArray = GetSpecialArrayName(array.ElementType, simple);
                if (specialArray != null)
                {
                    return nullable ? specialArray + "?" : specialArray;
                }

                if (simple)
                {
                    return GetTypeName(array.ElementType, true);
                }
                else if (noTypeArguments)
                {
                    return GetTypeName(array.ElementType, false, true);
                }
                else
                {
                    var x = GetTypeName(array.ElementType, false, false, true);

                    var arrayName = new StringBuilder();
                    for (int i = 0; i < array.Rank; i++)
                    {
                        arrayName.Append("Array<");
                    }
                    arrayName.Append(x);
                    for (int i = 0; i < array.Rank; i++)
                    {
                        arrayName.Append(">");
                        if (nullable) arrayName.Append("?");
                    }


                    return arrayName.ToString();
                }
            }

            if (type is IDynamicTypeSymbol)
            {
                return "system.Dynamic";
            }

            if (type is ITypeParameterSymbol)
            {
                if (nullable && IsNullable(type))
                {
                    return type.Name + "?";
                }

                return type.Name;
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_Char:
                    return "Char";
                case SpecialType.System_Byte:
                    return "UByte";
                case SpecialType.System_SByte:
                    return "Byte";
                case SpecialType.System_UInt16:
                    return "UShort";
                case SpecialType.System_Int16:
                    return "Short";
                case SpecialType.System_UInt32:
                    return "UInt";
                case SpecialType.System_Int32:
                    return "Int";
                case SpecialType.System_UInt64:
                    return "ULong";
                case SpecialType.System_Int64:
                    return "Long";
                case SpecialType.System_Void:
                    return "Unit";
                case SpecialType.System_Boolean:
                    return "Boolean";
                case SpecialType.System_Single:
                    return "Float";
                case SpecialType.System_Double:
                    return "Double";
                case SpecialType.System_String:
                    return nullable ? "String?" : "String";
                case SpecialType.System_Object:
                    return nullable ? "Any?" : "Any";
            }

            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                var nestedType = ((INamedTypeSymbol)type).TypeArguments[0];
                return GetTypeName(nestedType) + "?";
            }

            var nsAndName = GetNamespaceAndTypeName(type);
            if (simple && type.ContainingType != null)
            {
                var i = nsAndName.Item2.LastIndexOf('.');
                if (i > 0)
                {
                    nsAndName = Tuple.Create(nsAndName.Item1, nsAndName.Item2.Substring(i + 1));
                }
            }
            var fullName = nsAndName.Item1 + "." + nsAndName.Item2;
            var name = simple || string.IsNullOrEmpty(nsAndName.Item1) ? nsAndName.Item2 : fullName;

            if (type is INamedTypeSymbol named && named.IsGenericType)
            {
                var typeArgs = GetTypeArguments(named);

                if (name == "system.Action" || name == "system.Func" || TypeLookup.ContainsKey(fullName) && TypeLookup[fullName].Count > 1)
                {
                    name += typeArgs.Length;
                }

                if (!noTypeArguments)
                {
                    name += "<";

                    for (int i = 0; i < typeArgs.Length; i++)
                    {
                        if (i > 0) name += ", ";
                        name += GetTypeName(typeArgs[i], false, false);
                    }
                    name += ">";
                }
            }

            return name + (nullable && type.IsReferenceType ? "?" : "");
        }

        protected override string GetMethodNameInternal(IMethodSymbol method)
        {
            method = method.OriginalDefinition;

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

            switch (method.MethodKind)
            {
                case MethodKind.StaticConstructor:
                    return "";
                case MethodKind.Constructor:
                    return "constructor";
                case MethodKind.PropertyGet:
                    return ((IPropertySymbol)method.AssociatedSymbol).IsIndexer ? "get" : "get" + method.Name.Substring("get_".Length);
                case MethodKind.PropertySet:
                    return ((IPropertySymbol)method.AssociatedSymbol).IsIndexer ? "set" : "set" + method.Name.Substring("set_".Length);
                case MethodKind.EventAdd:
                    return "add" + method.Name.Substring("add_".Length);
                case MethodKind.EventRemove:
                    return "remove" + method.Name.Substring("remove_".Length);
            }

            return EscapeKeyword(method.Name.ToCamelCase());
        }

        protected override Dictionary<SpecialType, string> BuildSpecialArrayLookup()
        {
            return new Dictionary<SpecialType, string>
            {
                [SpecialType.System_SByte] = "ByteArray",
                [SpecialType.System_Byte] = "UByteArray",
                [SpecialType.System_Int16] = "ShortArray",
                [SpecialType.System_UInt16] = "UShortArray",
                [SpecialType.System_Int32] = "IntArray",
                [SpecialType.System_UInt32] = "UIntArray",
                [SpecialType.System_Int64] = "LongArray",
                [SpecialType.System_UInt64] = "ULongArray",
                [SpecialType.System_Decimal] = "DoubleArray",
                [SpecialType.System_Single] = "FloatArray",
                [SpecialType.System_Double] = "DoubleArray",
                [SpecialType.System_Boolean] = "BooleanArray",
                [SpecialType.System_Char] = "CharArray"

            };
        }

        protected override Tuple<string, string> GetNamespaceAndTypeName(ITypeSymbol type)
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

        private string BuildTypeName(ITypeSymbol type)
        {
            var s = type.Name;
            while (type.ContainingType != null)
            {
                s = type.ContainingType.Name + "." + s;
                type = type.ContainingType;
            }
            return s;
        }

        public string GetNamespace(ITypeSymbol type)
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

        public override string GetEventName(IEventSymbol eventSymbol)
        {
            return "__" + base.GetEventName(eventSymbol);
        }

        public override string GetDefaultValue(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Byte:
                    return "0.toUByte()";
                case SpecialType.System_UInt16:
                    return "0.toUShort()";
                case SpecialType.System_UInt32:
                    return "0.toUInt()";
                case SpecialType.System_UInt64:
                    return "0.toULong()";
                case SpecialType.System_UIntPtr:
                    return "0.toULong()";
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_IntPtr:
                    return "0";
                case SpecialType.System_Boolean:
                    return "false";
                case SpecialType.System_Char:
                    return "0";
                case SpecialType.System_Single:
                    return "0.0f";
                case SpecialType.System_Decimal:
                case SpecialType.System_Double:
                    return "0.0";
                default:
                    if (type.TypeKind == TypeKind.Enum)
                    {
                        var field = GetDefaultEnumField(type);
                        if (field != null)
                        {
                            return GetTypeName(type) + "." + GetFieldName(field);
                        }
                    }
                    return "null";
            }
        }

        public override string GetSymbolName(ISymbol symbol, BaseEmitterContext context)
        {
            if (symbol.Kind == SymbolKind.Parameter && context is KotlinEmitterContext kotlinEmitterContext)
            {
                if (kotlinEmitterContext.ParameterNames.TryGetValue((IParameterSymbol)symbol, out var name))
                {
                    return name;
                }
            }

            return base.GetSymbolName(symbol, context);
        }

        protected override string GetPropertyNameInternal(IPropertySymbol property)
        {
            return property.Name.ToCamelCase();
        }

        protected override string GetFieldNameInternal(IFieldSymbol field)
        {
            return field.Name.ToCamelCase();
        }

        public string GetArrayCreationFunctionName(ITypeSymbol elementType)
        {
            SpecialType specialType;
            if (elementType is IArrayTypeSymbol array)
            {
                specialType = (array.Sizes.Length > 0) ? SpecialType.None : array.ElementType.SpecialType;
            }
            else
            {
                specialType = elementType.SpecialType;
            }

            switch (specialType)
            {
                case SpecialType.System_SByte:
                    return "byteArrayOf";
                case SpecialType.System_Byte:
                    return "ubyteArrayOf";
                case SpecialType.System_Int16:
                    return "ushortArrayOf";
                case SpecialType.System_UInt16:
                    return "shortArrayOf";
                case SpecialType.System_Int32:
                    return "intArrayOf";
                case SpecialType.System_UInt32:
                    return "uintArrayOf";
                case SpecialType.System_Int64:
                    return "longArrayOf";
                case SpecialType.System_UInt64:
                    return "ulongArrayOf";
                case SpecialType.System_Boolean:
                    return "booleanArrayOf";
                case SpecialType.System_Char:
                    return "charArrayOf";
                case SpecialType.System_Single:
                    return "floatArrayOf";
                case SpecialType.System_Decimal:
                case SpecialType.System_Double:
                    return "doubleArrayOf";
                default:
                    return "arrayOf";
            }
        }

        protected override string EscapeKeyword(string symbolName)
        {
            switch (symbolName)
            {
                case "val":
                case "continue":
                    return "`" + symbolName + "`";
            }
            return base.EscapeKeyword(symbolName);
        }
    }
}
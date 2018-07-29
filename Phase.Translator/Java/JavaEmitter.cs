using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using NLog;
using Phase.Translator.Utils;

namespace Phase.Translator.Java
{
    public class JavaEmitter : BaseEmitter<JavaEmitterContext>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public ConcurrentDictionary<ITypeSymbol, PhaseType> NestedTypes { get; }

        public JavaEmitter(PhaseCompiler compiler)
            : base(compiler, Log)
        {
            NestedTypes = new ConcurrentDictionary<ITypeSymbol, PhaseType>();
        }

        protected override IEnumerable<JavaEmitterContext> BuildEmitterContexts(PhaseType type)
        {
            if (type.IsNested)
            {
                NestedTypes[type.TypeSymbol] = type;
                return Enumerable.Empty<JavaEmitterContext>();
            }
            return new[] { new JavaEmitterContext(this, type) };
        }

        public override string GetTypeName(ITypeSymbol type, bool simple = false, bool noTypeArguments = false)
        {
            return GetTypeName(type, simple, noTypeArguments, false);
        }

        public string GetTypeName(ITypeSymbol type, bool simple, bool noTypeArguments, bool isInTypeParameter)
        {
            if (type is IArrayTypeSymbol array)
            {
                var specialArray = GetSpecialArrayName(array.ElementType, simple);
                if (specialArray != null)
                {
                    return specialArray;
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
                    var x = GetTypeName(array.ElementType, false, false);
                    for (int i = 0; i < array.Rank; i++)
                    {
                        x += "[]";
                    }

                    return x;
                }
            }

            if (type is IDynamicTypeSymbol)
            {
                return "system.Dynamic";
            }

            if (type is ITypeParameterSymbol)
            {
                return type.Name;
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_Char:
                    return isInTypeParameter ? "Character" : "char";
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                    return isInTypeParameter ? "Byte" : "byte";
                case SpecialType.System_UInt16:
                case SpecialType.System_Int16:
                    return isInTypeParameter ? "Short" : "short";
                case SpecialType.System_UInt32:
                case SpecialType.System_Int32:
                    return isInTypeParameter ? "Integer" : "int";
                case SpecialType.System_UInt64:
                case SpecialType.System_Int64:
                    return isInTypeParameter ? "Long" : "long";
                case SpecialType.System_Void:
                    return "void";
                case SpecialType.System_Boolean:
                    return isInTypeParameter ? "Boolean ": "boolean";
                case SpecialType.System_Single:
                    return isInTypeParameter ? "Float" : "float";
                case SpecialType.System_Double:
                    return isInTypeParameter ? "Double ": "double";
                case SpecialType.System_String:
                    return "String";
                case SpecialType.System_Object:
                    return "Object";
            }

            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                var nestedType = ((INamedTypeSymbol)type).TypeArguments[0];
                switch (nestedType.SpecialType)
                {
                    case SpecialType.System_Char:
                        return "Character";
                    case SpecialType.System_Byte:
                    case SpecialType.System_SByte:
                        return "Byte";
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Int16:
                        return "Short";
                    case SpecialType.System_UInt32:
                    case SpecialType.System_Int32:
                        return "Integer";
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Int64:
                        return "Long";
                    case SpecialType.System_Void:
                        return "Void";
                    case SpecialType.System_Boolean:
                        return "Boolean";
                    case SpecialType.System_Single:
                        return "Float";
                    case SpecialType.System_Double:
                        return "Double";
                    default:
                        return GetTypeName(nestedType);
                }
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

                if (!noTypeArguments && !simple)
                {
                    name += "<";

                    for (int i = 0; i < typeArgs.Length; i++)
                    {
                        if (i > 0) name += ", ";
                        name += GetTypeName(typeArgs[i], false, false, true);
                    }
                    name += ">";
                }
            }

            return name;
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
                    return GetTypeName(method.ContainingType, true, true);
                case MethodKind.PropertyGet:
                    return "get" + method.Name.Substring("get_".Length);
                case MethodKind.PropertySet:
                    return "set" + method.Name.Substring("Set_".Length);
                case MethodKind.EventAdd:
                    return "add" + method.Name.Substring("add_".Length);
                case MethodKind.EventRemove:
                    return "remove" + method.Name.Substring("remove_".Length);
            }

            var numCaps = 0;
            foreach (var c in method.Name)
            {
                if (char.IsUpper(c)) numCaps++;
                else break;
            }

            return numCaps == method.Name.Length
                ? method.Name.ToLowerInvariant()
                : method.Name.Substring(0, numCaps).ToLowerInvariant() + method.Name.Substring(numCaps);
        }

        protected override Dictionary<SpecialType, string> BuildSpecialArrayLookup()
        {
            return new Dictionary<SpecialType, string>();
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
    }
}
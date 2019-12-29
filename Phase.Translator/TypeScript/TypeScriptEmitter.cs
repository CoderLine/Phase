using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.CodeAnalysis;
using NLog;
using Phase.Translator.Utils;

namespace Phase.Translator.TypeScript
{
    public class TypeScriptEmitter : BaseEmitter<TypeScriptEmitterContext>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ConcurrentDictionary<string, IMethodSymbol> _reservedMethodNames;

        public TypeScriptEmitter(PhaseCompiler compiler)
            : base(compiler, Log)
        {
            _reservedMethodNames = new ConcurrentDictionary<string, IMethodSymbol>();
        }


        public string GetFileName(ITypeSymbol type, bool includeExtension) =>
            GetFileName(type, includeExtension, Path.DirectorySeparatorChar);

        public string GetFileName(ITypeSymbol type, bool includeExtension, char directorySeparator)
        {
            var nsAndName = GetNamespaceAndTypeName(type);
            var name = nsAndName.Item1 + "." + nsAndName.Item2;
            var p = name.IndexOf("<");
            if (p >= 0) name = name.Substring(0, p);
            return name.Replace('.', directorySeparator) + (includeExtension ? ".ts" : "");
        }

        protected override IEnumerable<TypeScriptEmitterContext> BuildEmitterContexts(PhaseType type)
        {
            return new[] {new TypeScriptEmitterContext(this, type)};
        }


        public string GetTypeNameWithNullability(ITypeSymbol type, bool simple = false, bool noTypeArguments = false)
        {
            var name = GetTypeName(type, simple, noTypeArguments);
            // if (!simple && !noTypeArguments && (IsNullable(type) || type.IsReferenceType ||
            //                                     type.SpecialType == SpecialType.System_String))
            // {
            //     name += " | null";
            // }

            return name;
        }

        public override string GetTypeName(ITypeSymbol type, bool simple = false, bool noTypeArguments = false)
        {
            if (type is IArrayTypeSymbol array)
            {
                var specialArray = GetSpecialArrayName(array.ElementType, simple);
                if (specialArray != null)
                {
                    return specialArray;
                }

                return simple
                    ? GetTypeName(array.ElementType, true) + "Array"
                    : "Array<" + GetTypeName(array.ElementType) + ">";
            }

            if (type is IDynamicTypeSymbol)
            {
                return "any";
            }

            if (type is ITypeParameterSymbol)
            {
                return type.Name;
            }

            if (type.SpecialType == SpecialType.System_Void)
            {
                return "void";
            }

            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                return GetTypeName(((INamedTypeSymbol) type).TypeArguments[0], simple, noTypeArguments);
            }

            var nsAndName = GetNamespaceAndTypeName(type);
            var fullName = nsAndName.Item1 + "." + nsAndName.Item2;
            var name = nsAndName.Item2;

            if (type is INamedTypeSymbol named && named.IsGenericType)
            {
                var typeArgs = GetTypeArguments(named);

                if (fullName == "system.Action" || fullName == "system.Func" ||
                    TypeLookup.TryGetValue(fullName, out var types) && types.Count(t => !IsExternal(t)) > 1)
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

        protected override string GetMethodNameInternal(IMethodSymbol method, BaseEmitterContext context)
        {
            method = method.OriginalDefinition;

            if (method.MethodKind == MethodKind.StaticConstructor)
            {
                return "__init__";
            }

            if (!method.ExplicitInterfaceImplementations.IsEmpty)
            {
                var impl = method.ExplicitInterfaceImplementations[0];
                return GetTypeName(impl.ContainingType, true) + "_" + GetMethodName(impl, context);
            }

            var attributeName = GetNameFromAttribute(method);
            if (!string.IsNullOrEmpty(attributeName))
            {
                return attributeName;
            }

            if (method.OverriddenMethod != null)
            {
                return GetMethodName(method.OverriddenMethod, context);
            }

            var x = new StringBuilder();
            if (method.MethodKind == MethodKind.Constructor)
            {
                if (HasNativeConstructors(method.ContainingType) || !HasConstructorOverloads(method.ContainingType))
                {
                    return "constructor";
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
                    return GetPropertyName(prop);
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
                    return GetPropertyName(prop);
                }
            }
            else if (method.MethodKind == MethodKind.EventAdd)
            {
                x.Append("add" + GetSymbolName(method.AssociatedSymbol, context).ToPascalCase());
            }
            else if (method.MethodKind == MethodKind.EventRemove)
            {
                x.Append("remove" + GetSymbolName(method.AssociatedSymbol, context).ToPascalCase());
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

                    if (p.Type is INamedTypeSymbol named && named.IsGenericType)
                    {
                        foreach (var t in named.TypeArguments)
                        {
                            x.Append("_");
                            name = GetTypeName(t, true);
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

        protected override Dictionary<SpecialType, string> BuildSpecialArrayLookup()
        {
            return new Dictionary<SpecialType, string>
            {
                [SpecialType.System_SByte] = "Int8Array",
                [SpecialType.System_Byte] = "Uint8Array",
                [SpecialType.System_Int16] = "Int16Array",
                [SpecialType.System_UInt16] = "Uint16Array",
                [SpecialType.System_Int32] = "Int32Array",
                [SpecialType.System_UInt32] = "Uint32Array",
                [SpecialType.System_Int64] = "Int64Array",
                [SpecialType.System_UInt64] = "Uint64Array",
                [SpecialType.System_Decimal] = "Float64Array",
                [SpecialType.System_Single] = "Float32Array",
                [SpecialType.System_Double] = "Float64Array"
            };
        }

        protected override Tuple<string, string> GetNamespaceAndTypeName(ITypeSymbol type)
        {
            var nameAttribute = GetAttributes(type)
                .FirstOrDefault(a => a.AttributeClass.Equals(GetPhaseType("Phase.Attributes.NameAttribute")));
            if (nameAttribute != null)
            {
                var name = nameAttribute.ConstructorArguments[0].Value.ToString();
                var keepNamespace = nameAttribute.ConstructorArguments.Length > 1
                    ? (bool) nameAttribute.ConstructorArguments[1].Value
                    : false;
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

        public bool AreTypesEqual(ITypeSymbol a, ITypeSymbol b)
        {
            if (a.Equals(b))
            {
                return true;
            }

            if (a.TypeKind != b.TypeKind)
            {
                return false;
            }

            switch (a.TypeKind)
            {
                case TypeKind.Array:
                    var aat = (IArrayTypeSymbol) a;
                    var bat = (IArrayTypeSymbol) a;
                    if (aat.Rank != bat.Rank)
                    {
                        return false;
                    }

                    if (aat.Sizes.Length != bat.Sizes.Length)
                    {
                        return false;
                    }

                    if (aat.Sizes.Where((t, i) => t != bat.Sizes[i]).Any())
                    {
                        return false;
                    }

                    return AreTypesEqual(aat.ElementType, bat.ElementType);
                default:
                    return SymbolEquivalenceComparer.Instance.Equals(a, b);
            }
        }

        public override string GetDefaultValue(ITypeSymbol type)
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
                            return GetTypeName(type) + "." + GetFieldName(field);
                        }
                    }

                    return "null";
            }
        }
    }
}
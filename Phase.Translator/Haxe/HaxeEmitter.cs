using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using NLog;
using Phase.Translator.Utils;

namespace Phase.Translator.Haxe
{
    public class HaxeEmitter : BaseEmitter<HaxeEmitterContext>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ConcurrentDictionary<string, IMethodSymbol> _reservedMethodNames;

        public HaxeEmitter(PhaseCompiler compiler)
            : base(compiler, Log)
        {
            _reservedMethodNames = new ConcurrentDictionary<string, IMethodSymbol>();
        }

        protected override IEnumerable<HaxeEmitterContext> BuildEmitterContexts(PhaseType type)
        {
            return new[]{new HaxeEmitterContext(this, type)};
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

                return simple ? GetTypeName(array.ElementType, true) + "Array" : "system.FixedArray<" + GetTypeName(array.ElementType) + ">";
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

        protected override bool IsKeyWord(string name)
        {
            switch (name)
            {
                case "dynamic":
                    return true;
            }
            return false;
        }

        protected override string GetMethodNameInternal(IMethodSymbol method)
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
                [SpecialType.System_SByte] = "system.SByteArray",
                [SpecialType.System_Byte] = "system.ByteArray",
                [SpecialType.System_Int16] = "system.Int16Array",
                [SpecialType.System_UInt16] = "system.UInt16Array",
                [SpecialType.System_Int32] = "system.Int32Array",
                [SpecialType.System_UInt32] = "system.UInt32Array",
                [SpecialType.System_Int64] = "system.Int64Array",
                [SpecialType.System_UInt64] = "system.UInt64Array",
                [SpecialType.System_Decimal] = "system.DecimalArray",
                [SpecialType.System_Single] = "system.SingleArray",
                [SpecialType.System_Double] = "system.DoubleArray"
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
    }
}
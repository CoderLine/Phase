using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using NLog;
using Phase.Translator.Utils;

namespace Phase.Translator.Cpp
{
    public class CppEmitter : BaseEmitter<CppEmitterContext>
    {
        public const string SystemPrefix = "phase";
        public const string FileExtensionHeader = ".h";
        public const string FileExtensionSource = ".cpp";

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public CppEmitter(PhaseCompiler compiler)
            : base(compiler, Log)
        {
        }

        protected override IEnumerable<CppEmitterContext> BuildEmitterContexts(PhaseType type)
        {
            switch (type.Kind)
            {
                case PhaseTypeKind.Class:
                    if (type.TypeSymbol.IsGenericType)
                    {
                        return new CppEmitterContext[]
                        {
                            new CppEmitterContext<ClassHeaderBlock>(this, type, FileExtensionHeader)
                        };
                    }
                    else
                    {
                        return new CppEmitterContext[]
                        {
                            new CppEmitterContext<ClassHeaderBlock>(this, type, FileExtensionHeader),
                            new CppEmitterContext<ClassSourceBlock>(this, type, FileExtensionSource)
                        };
                    }
                case PhaseTypeKind.Interface:
                    return new CppEmitterContext[]
                    {
                        new CppEmitterContext<ClassHeaderBlock>(this, type, FileExtensionHeader),
                    };
                case PhaseTypeKind.Enum:
                    return new CppEmitterContext[]
                    {
                        new CppEmitterContext<EnumHeaderBlock>(this, type, FileExtensionHeader),
                        //new CppEmitterContext<EnumSourceBlock>(this, type,FileExtensionSource)
                    };
                case PhaseTypeKind.Struct:
                    if (type.TypeSymbol.IsGenericType)
                    {
                        return new CppEmitterContext[]
                        {
                            new CppEmitterContext<ClassHeaderBlock>(this, type, FileExtensionHeader),
                        };
                    }
                    else
                    {
                        return new CppEmitterContext[]
                        {
                            new CppEmitterContext<StructHeaderBlock>(this, type, FileExtensionHeader),
                            new CppEmitterContext<StructSourceBlock>(this, type, FileExtensionSource)
                        };
                    }
                case PhaseTypeKind.Delegate:
                    return new[]
                    {
                        new CppEmitterContext<DelegateHeaderBlock>(this, type, FileExtensionHeader)
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override string GetTypeName(ITypeSymbol type, bool simple = false, bool noTypeArguments = false)
        {
            return GetTypeName(type, simple, noTypeArguments);
        }

        public enum TypeNamePointerKind
        {
            NoPointer,
            SharedPointerDeclaration,
            SharedPointerUsage,
            WeakPointerDeclaration,
            WeakPointerUsage
        }

        public string GetTypeName(ITypeSymbol type, bool simple = false, bool noTypeArguments = false, TypeNamePointerKind pointerKind = TypeNamePointerKind.SharedPointerUsage)
        {
            if (type is IArrayTypeSymbol array)
            {
                var specialArray = GetSpecialArrayName(array.ElementType, simple);
                if (specialArray != null)
                {
                    return specialArray;
                }

                TypeNamePointerKind typeArgumentKind = TypeNamePointerKind.NoPointer;
                switch (pointerKind)
                {
                    case TypeNamePointerKind.NoPointer:
                        typeArgumentKind = TypeNamePointerKind.SharedPointerDeclaration;
                        break;
                    case TypeNamePointerKind.SharedPointerDeclaration:
                        typeArgumentKind = TypeNamePointerKind.SharedPointerDeclaration;
                        break;
                    case TypeNamePointerKind.SharedPointerUsage:
                        typeArgumentKind = TypeNamePointerKind.SharedPointerDeclaration;
                        break;
                    case TypeNamePointerKind.WeakPointerDeclaration:
                        typeArgumentKind = TypeNamePointerKind.WeakPointerDeclaration;
                        break;
                    case TypeNamePointerKind.WeakPointerUsage:
                        typeArgumentKind = TypeNamePointerKind.WeakPointerDeclaration;
                        break;
                }

                return "Phase::Array<" + GetTypeName(array.ElementType, false, false, typeArgumentKind) + ">";
            }

            if (type is IDynamicTypeSymbol)
            {
                string objectName = "System::Object";
                switch (pointerKind)
                {
                    case TypeNamePointerKind.NoPointer:
                        return objectName;
                    case TypeNamePointerKind.SharedPointerDeclaration:
                        return $"std::shared_ptr<{objectName}>";
                    case TypeNamePointerKind.SharedPointerUsage:
                        return $"std::shared_ptr<{objectName}>";
                    case TypeNamePointerKind.WeakPointerDeclaration:
                        return $"std::weak_ptr<{objectName}>";
                    case TypeNamePointerKind.WeakPointerUsage:
                        return $"std::weak_ptr<{objectName}>";
                    default:
                        throw new ArgumentOutOfRangeException(nameof(pointerKind), pointerKind, null);
                }
            }

            if (type is ITypeParameterSymbol)
            {
                return type.Name;
            }

            if (type.SpecialType == SpecialType.System_Void)
            {
                return "void";
            }

            if (type.IsValueType || type.TypeKind == TypeKind.Delegate || type.SpecialType == SpecialType.System_String)
            {
                pointerKind = TypeNamePointerKind.NoPointer;
            }

            var nsAndName = GetNamespaceAndTypeName(type);

            var fullName = nsAndName.Item1.Replace(".", "::") + "::" + nsAndName.Item2;
            var name = new StringBuilder();

            switch (pointerKind)
            {
                case TypeNamePointerKind.SharedPointerUsage:
                    name.Append("std::shared_ptr<");
                    break;
                case TypeNamePointerKind.SharedPointerDeclaration:
                    name.Append("std::shared_ptr<");
                    break;
                case TypeNamePointerKind.WeakPointerUsage:
                    name.Append("std::weak_ptr<");
                    break;
                case TypeNamePointerKind.WeakPointerDeclaration:
                    name.Append("std::weak_ptr<");
                    break;
            }

            name.Append(simple || string.IsNullOrEmpty(nsAndName.Item1) ? nsAndName.Item2 : fullName);

            if (type is INamedTypeSymbol named && named.IsGenericType)
            {
                var typeArgs = GetTypeArguments(named);

                if (fullName == "System::Action" || fullName == "System::Func" || TypeLookup.ContainsKey(fullName) && TypeLookup[fullName].Count > 1)
                {
                    name.Append(typeArgs.Length);
                }

                if (!noTypeArguments && !simple)
                {
                    name.Append("<");

                    TypeNamePointerKind typeArgumentKind = TypeNamePointerKind.NoPointer;
                    switch (pointerKind)
                    {
                        case TypeNamePointerKind.NoPointer:
                            typeArgumentKind = TypeNamePointerKind.SharedPointerDeclaration;
                            break;
                        case TypeNamePointerKind.SharedPointerDeclaration:
                            typeArgumentKind = TypeNamePointerKind.SharedPointerDeclaration;
                            break;
                        case TypeNamePointerKind.SharedPointerUsage:
                            typeArgumentKind = TypeNamePointerKind.SharedPointerDeclaration;
                            break;
                        case TypeNamePointerKind.WeakPointerDeclaration:
                            typeArgumentKind = TypeNamePointerKind.WeakPointerDeclaration;
                            break;
                        case TypeNamePointerKind.WeakPointerUsage:
                            typeArgumentKind = TypeNamePointerKind.WeakPointerDeclaration;
                            break;
                    }


                    for (int i = 0; i < typeArgs.Length; i++)
                    {
                        if (i > 0) name.Append(", ");
                        name.Append(GetTypeName(typeArgs[i], false, false, typeArgumentKind));
                    }
                    name.Append(">");
                }
            }

            switch (pointerKind)
            {
                case TypeNamePointerKind.SharedPointerUsage:
                    name.Append(">");
                    break;
                case TypeNamePointerKind.SharedPointerDeclaration:
                    name.Append(">");
                    break;
                case TypeNamePointerKind.WeakPointerUsage:
                    name.Append(">");
                    break;
                case TypeNamePointerKind.WeakPointerDeclaration:
                    name.Append(">");
                    break;
            }

            return name.ToString();
        }


        protected override string GetMethodNameInternal(IMethodSymbol method, BaseEmitterContext context)
        {
            method = method.OriginalDefinition;

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

            switch (method.MethodKind)
            {
                case MethodKind.StaticConstructor:
                    return "cctor_" + GetTypeName(method.ContainingType, true, true, TypeNamePointerKind.NoPointer);
                case MethodKind.Constructor:
                    return GetTypeName(method.ContainingType, true, true, TypeNamePointerKind.NoPointer);
                case MethodKind.PropertyGet:
                    return "Get" + method.Name.Substring("get_".Length);
                case MethodKind.PropertySet:
                    return "Set" + method.Name.Substring("Set_".Length);
                case MethodKind.EventAdd:
                    return "Add" + method.Name.Substring("add_".Length);
                case MethodKind.EventRemove:
                    return "Remove" + method.Name.Substring("remove_".Length);
            }

            return method.Name;
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
                nss.Add(SafeName(ns.Name));
                ns = ns.ContainingNamespace;
            }
            nss.Reverse();
            return string.Join(".", nss);
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
                            return GetTypeName(type, false, false, TypeNamePointerKind.NoPointer) + "::" + field.Name;
                        }
                    }
                    return "nullptr";
            }
        }

        public override bool IsMethodRedirected(IMethodSymbol methodSymbol, out string typeName)
        {
            switch (methodSymbol.ContainingType.SpecialType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_SByte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                    typeName = GetTypeName(methodSymbol.ContainingType, false, true, TypeNamePointerKind.NoPointer) + "Extensions";
                    return true;

            }

            return base.IsMethodRedirected(methodSymbol, out typeName);
        }
    }
}
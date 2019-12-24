using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Cpp
{
    class MethodHeaderBlock : AbstractCppEmitterBlock
    {
        private readonly IMethodSymbol _method;

        public MethodHeaderBlock(CppEmitterContext emitterContext, IMethodSymbol method)
        {
            _method = method;
            Init(emitterContext);
        }

        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Emitter.IsExternal(_method)
                || Emitter.IsCompilerExtension(_method)
                || (_method.MethodKind == MethodKind.PropertyGet && Emitter.IsExternal(_method.AssociatedSymbol))
                || (_method.MethodKind == MethodKind.PropertySet && Emitter.IsExternal(_method.AssociatedSymbol))
            )
            {
                return;
            }

            if (!_method.ExplicitInterfaceImplementations.IsEmpty
                || (_method.MethodKind == MethodKind.PropertyGet && !((IPropertySymbol)_method.AssociatedSymbol).ExplicitInterfaceImplementations.IsEmpty)
                || (_method.MethodKind == MethodKind.PropertySet && !((IPropertySymbol)_method.AssociatedSymbol).ExplicitInterfaceImplementations.IsEmpty)
            )
            {
                return;
            }

            if (_method.MethodKind == MethodKind.StaticConstructor && _method.DeclaringSyntaxReferences.Length == 0)
            {
                // implicit static constructor
                return;
            }

            if (_method.ContainingType.TypeKind == TypeKind.Struct && Emitter.IsAbstract(_method.ContainingType) && _method.MethodKind == MethodKind.Constructor && _method.DeclaringSyntaxReferences.Length == 0)
            {
                // implicit constructor for structs
                return;
            }

            WriteComments(_method, true, cancellationToken);

            WriteAccessibility(_method.DeclaredAccessibility);

            var typeParameters = new List<ITypeSymbol>(_method.TypeParameters);
            if (_method.IsStatic)
            {
                CollectTypeParameters(typeParameters, _method.ReturnType);
                foreach (var parameter in _method.Parameters)
                {
                    CollectTypeParameters(typeParameters, parameter.Type);
                }
            }

            if (typeParameters.Count > 0)
            {
                Write("template <");

                for (int i = 0; i < typeParameters.Count; i++)
                {
                    if (i > 0)
                    {
                        WriteComma();
                    }
                    Write("typename ", typeParameters[i].Name);
                }

                Write(">");
                WriteNewLine();
            }

            if (_method.IsStatic)
            {
                Write("static ");
            }

            if (Emitter.IsInline(_method))
            {
                Write("inline ");
            }

            if (_method.IsVirtual && _method.OverriddenMethod == null || _method.ContainingType.TypeKind == TypeKind.Interface || _method.IsAbstract)
            {
                Write("virtual ");
            }

            switch (_method.MethodKind)
            {
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                    WriteType(((IPropertySymbol)_method.AssociatedSymbol).Type);
                    EmitterContext.ImportType(((IPropertySymbol)_method.AssociatedSymbol).Type);
                    Write(" ");
                    break;
                case MethodKind.Constructor:
                    break;
                default:
                    WriteType(_method.ReturnType);
                    EmitterContext.ImportType(_method.ReturnType);
                    Write(" ");
                    break;
            }

            var methodName = EmitterContext.GetMethodName(_method);
            Write(methodName);

            WriteOpenParentheses();
            WriteParameterDeclarations(_method.Parameters, true, cancellationToken);
            WriteCloseParentheses();

            if (_method.OverriddenMethod != null && _method.OverriddenMethod.ContainingType.SpecialType != SpecialType.System_Object && !Emitter.IsAbstract(_method.ContainingType))
            {
                Write("override ");
            }

            var isAbstract = _method.IsAbstract ||
                             (_method.AssociatedSymbol != null && _method.AssociatedSymbol.IsAbstract);

            if (!isAbstract && (_method.IsGenericMethod || _method.ContainingType.IsGenericType))
            {
                WriteMethodBody(_method, cancellationToken);
            }
            else if (_method.ContainingType.TypeKind == TypeKind.Interface || isAbstract)
            {
                Write(" = 0");
                WriteSemiColon(true);
            }
            else
            {
                switch (_method.MethodKind)
                {
                    case MethodKind.PropertyGet:
                    case MethodKind.PropertySet:
                        if (Emitter.IsAutoProperty((IPropertySymbol)_method.AssociatedSymbol))
                        {
                            WriteMethodBody(_method, cancellationToken);
                        }
                        else
                        {
                            WriteSemiColon(true);
                        }
                        break;
                    case MethodKind.EventAdd:
                    case MethodKind.EventRemove:
                    case MethodKind.EventRaise:
                        if (Emitter.IsEventField((IEventSymbol)_method.AssociatedSymbol))
                        {
                            WriteMethodBody(_method, cancellationToken);
                        }
                        else
                        {
                            WriteSemiColon(true);
                        }
                        break;
                    default:
                        WriteSemiColon(true);
                        break;
                }
            }
            WriteNewLine();
        }

        private void CollectTypeParameters(List<ITypeSymbol> typeParameters, ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.TypeParameter)
            {
                if (!typeParameters.Contains(type))
                {
                    typeParameters.Add(type);
                }
            }
            else if (type is INamedTypeSymbol)
            {
                var named = (INamedTypeSymbol)type;
                foreach (var argument in named.TypeArguments)
                {
                    CollectTypeParameters(typeParameters, argument);
                }
            }
        }
    }
}
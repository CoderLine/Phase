using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Cpp
{
    class MethodSourceBlock : AbstractCppEmitterBlock
    {
        private readonly IMethodSymbol _method;

        public MethodSourceBlock(CppEmitterContext emitterContext, IMethodSymbol method)
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

            switch (_method.MethodKind)
            {
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                    if (Emitter.IsAutoProperty((IPropertySymbol)_method.AssociatedSymbol))
                    {
                        EmitterContext.ImportType(((IPropertySymbol)_method.AssociatedSymbol).Type);
                        return;
                    }
                    break;
                case MethodKind.EventAdd:
                case MethodKind.EventRemove:
                case MethodKind.EventRaise:
                    if (Emitter.IsEventField((IEventSymbol)_method.AssociatedSymbol))
                    {
                        EmitterContext.ImportType(((IEventSymbol)_method.AssociatedSymbol).Type);
                        return;
                    }
                    break;
            }

            var isAbstract = _method.IsAbstract ||
                             (_method.AssociatedSymbol != null && _method.AssociatedSymbol.IsAbstract);

            if (_method.IsGenericMethod || isAbstract)
            {
                return;
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

            var typeName = Emitter.GetTypeName(_method.ContainingType, false, true, CppEmitter.TypeNamePointerKind.NoPointer);
            var methodName = Emitter.GetMethodName(_method);

            Write(typeName);
            Write("::");
            Write(methodName);

            WriteOpenParentheses();
            WriteParameterDeclarations(_method.Parameters, false, cancellationToken);
            WriteCloseParentheses();

            WriteNewLine();

            WriteMethodBody(_method, cancellationToken);

            WriteNewLine();
        }
    }
}
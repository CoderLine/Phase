using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Haxe
{
    public class PropertyBlock : AbstractHaxeScriptEmitterBlock
    {
        private readonly IPropertySymbol _property;

        public PropertyBlock(HaxeEmitterContext context, IPropertySymbol property)
            : base(context)
        {
            _property = property;
        }

        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Emitter.IsExternal(_property))
            {
                return;
            }

            if (!_property.ExplicitInterfaceImplementations.IsEmpty)
            {
                return;
            }


            if (!_property.IsIndexer && _property.OverriddenProperty == null)
            {
                WriteComments(_property, cancellationToken);
                WriteMeta(_property, cancellationToken);

                var isAutoProperty = Emitter.IsAutoProperty(_property);

                WriteAccessibility(_property.DeclaredAccessibility);

                if (_property.IsStatic)
                {
                    Write("static ");
                }

                var propertyName = Emitter.GetPropertyName(_property);
                Write("var ", propertyName);

                WriteOpenParentheses();
                if (_property.GetMethod != null)
                {
                    Write(isAutoProperty ? "default" : "get");
                }
                else
                {
                    Write("never");
                }
                Write(", ");

                if (_property.SetMethod != null)
                {
                    Write(isAutoProperty ? "default" : "set");
                }
                else if(Emitter.IsAbstract(_property.ContainingType))
                {
                    Write("never");
                }
                else
                {
                    Write(isAutoProperty ? "null" : "never");
                }

                WriteCloseParentheses();

                WriteSpace();
                WriteColon();
                WriteType(_property.Type);
                WriteSemiColon(true);
                WriteNewLine();

                WriteComments(_property, false, cancellationToken);
            }
        }
    }
}
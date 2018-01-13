using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Haxe
{
    public class PropertyBlock : AbstractHaxeScriptEmitterBlock
    {
        private readonly IPropertySymbol _property;

        public PropertyBlock(HaxeEmitter emitter, IPropertySymbol property)
            : base(emitter)
        {
            _property = property;
        }

        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Emitter.IsExternal(_property))
            {
                return;
            }

            if (!_property.ExplicitInterfaceImplementations.IsEmpty)
            {
                return;
            }

            Func<int, int> x = null;
            x += i => 10;
            x += i => 20;
            var y = x(10);

            var isAutoProperty = Emitter.IsAutoProperty(_property);

            if (!_property.IsIndexer && _property.OverriddenProperty == null)
            {
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
                else
                {
                    Write("never");
                }

                WriteCloseParentheses();

                WriteSpace();
                WriteColon();
                WriteType(_property.Type);
                WriteSemiColon(true);
                WriteNewLine();
            }
        }
    }
}
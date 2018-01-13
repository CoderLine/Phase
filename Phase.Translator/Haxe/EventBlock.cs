using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Haxe
{
    public class EventBlock : AbstractHaxeScriptEmitterBlock
    {
        private readonly IEventSymbol _eventSymbol;

        public EventBlock(HaxeEmitter emitter, IEventSymbol eventSymbol)
            : base(emitter)
        {
            _eventSymbol = eventSymbol;
        }

        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {

            WriteAccessibility(_eventSymbol.DeclaredAccessibility);

            if (_eventSymbol.IsStatic)
            {
                Write("static ");
            }

            var propertyName = Emitter.GetEventName(_eventSymbol);
            Write("var ", propertyName);

            WriteSpace();
            WriteColon();

            var delegateMethod = ((INamedTypeSymbol)_eventSymbol.Type).DelegateInvokeMethod;

            Write("system.Event");
            if (delegateMethod.ReturnsVoid)
            {
                Write("Action");
            }
            else
            {
                Write("Func");
            }

            if (delegateMethod.Parameters.Length > 0 || !delegateMethod.ReturnsVoid)
            {
                Write(delegateMethod.Parameters.Length, "<");

                for (int i = 0; i < delegateMethod.Parameters.Length; i++)
                {
                    if (i > 0) WriteComma();
                    WriteType(delegateMethod.Parameters[i].Type);
                }

                if (!delegateMethod.ReturnsVoid)
                {
                    if (delegateMethod.Parameters.Length > 0) WriteComma();
                    WriteType(delegateMethod.ReturnType);
                }

                Write(">");
            }

            WriteSemiColon(true);
            WriteNewLine();
        }
    }
}
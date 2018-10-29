using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Kotlin
{
    public class EventBlock : AbstractKotlinEmitterBlock
    {
        private readonly IEventSymbol _eventSymbol;

        public EventBlock(KotlinEmitterContext context, IEventSymbol eventSymbol)
            : base(context)
        {
            _eventSymbol = eventSymbol;
        }

        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteComments(_eventSymbol, cancellationToken);

            if (_eventSymbol.IsStatic)
            {
                Write("@JvmStatic");
                WriteNewLine();
            }

            WriteAccessibility(Accessibility.Private);

            Write(" var ");

            var propertyName = Emitter.GetEventName(_eventSymbol);
            Write(" ", propertyName);

            Write(" : ");
            WriteEventType(((INamedTypeSymbol)_eventSymbol.Type));

            Write(" = null");
            WriteSemiColon(true);

            WriteComments(_eventSymbol, false, cancellationToken);
        }
    }
}
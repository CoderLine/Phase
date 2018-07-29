using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Java
{
    public class EventBlock : AbstractJavaEmitterBlock
    {
        private readonly IEventSymbol _eventSymbol;

        public EventBlock(JavaEmitterContext context, IEventSymbol eventSymbol)
            : base(context)
        {
            _eventSymbol = eventSymbol;
        }

        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteComments(_eventSymbol, cancellationToken);

            WriteAccessibility(Accessibility.Private);

            if (_eventSymbol.IsStatic)
            {
                Write("static ");
            }

            WriteEventType(((INamedTypeSymbol)_eventSymbol.Type));

            var propertyName = Emitter.GetEventName(_eventSymbol);
            Write(" ", propertyName);

            WriteSemiColon(true);
            WriteNewLine();

            WriteComments(_eventSymbol, false, cancellationToken);
        }
    }
}
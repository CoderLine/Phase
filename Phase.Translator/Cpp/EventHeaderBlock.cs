using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Cpp
{
    class EventHeaderBlock : AbstractCppEmitterBlock
    {
        private readonly IEventSymbol _eventSymbol;

        public EventHeaderBlock(CppEmitterContext emitterContext, IEventSymbol eventSymbol)
        {
            _eventSymbol = eventSymbol;
            Init(emitterContext);
        }

        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteComments(_eventSymbol, cancellationToken);

            WriteAccessibility(_eventSymbol.DeclaredAccessibility);

            if (_eventSymbol.IsStatic)
            {
                Write("static ");
            }

            var fieldName = Emitter.GetEventName(_eventSymbol);
            WriteEventType(((INamedTypeSymbol)_eventSymbol.Type));
            WriteSpace();
            Write(fieldName);
            WriteSemiColon(true);
        }
    }
}
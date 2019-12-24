using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.TypeScript
{
    public class EventBlock : AbstractTypeScriptEmitterBlock
    {
        private readonly IEventSymbol _eventSymbol;

        public EventBlock(TypeScriptEmitterContext context, IEventSymbol eventSymbol)
            : base(context)
        {
            _eventSymbol = eventSymbol;
        }

        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteComments(_eventSymbol, cancellationToken);
            WriteMeta(_eventSymbol, cancellationToken);

            WriteAccessibility(_eventSymbol.DeclaredAccessibility);

            if (_eventSymbol.IsStatic)
            {
                Write("static ");
            }

            var propertyName = Emitter.GetEventName(_eventSymbol);
            Write("var ", propertyName);

            WriteSpace();
            WriteColon();

            WriteEventType(((INamedTypeSymbol) _eventSymbol.Type));
            

            WriteSemiColon(true);
            WriteNewLine();

            WriteComments(_eventSymbol, false, cancellationToken);
        }
    }
}
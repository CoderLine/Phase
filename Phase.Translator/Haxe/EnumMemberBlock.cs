using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Haxe
{
    public class EnumMemberBlock : AbstractHaxeScriptEmitterBlock
    {
        private readonly IFieldSymbol _field;

        public EnumMemberBlock(HaxeEmitterContext context, IFieldSymbol field)
            : base(context)
        {
            _field = field;
        }

        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteComments(_field, cancellationToken);

            WriteVar();
            Write(_field.Name);
            Write(" = ");
            Write(_field.ConstantValue);
            WriteSemiColon(true);

            WriteComments(_field, false, cancellationToken);
        }
    }
}
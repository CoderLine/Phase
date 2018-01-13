using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Haxe
{
    public class EnumMemberBlock : AbstractHaxeScriptEmitterBlock
    {
        private readonly IFieldSymbol _field;

        public EnumMemberBlock(HaxeEmitter emitter, IFieldSymbol field)
            : base(emitter)
        {
            _field = field;
        }

        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteVar();
            Write(_field.Name);
            Write(" = ");
            Write(_field.ConstantValue);
            WriteSemiColon(true);
        }
    }
}
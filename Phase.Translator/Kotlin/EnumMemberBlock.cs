using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Kotlin
{
    public class EnumMemberBlock : AbstractKotlinEmitterBlock
    {
        private readonly IFieldSymbol _field;

        public EnumMemberBlock(KotlinEmitterContext context, IFieldSymbol field)
            : base(context)
        {
            _field = field;
        }

        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteComments(_field, cancellationToken);

            Write(_field.Name);
            WriteOpenParentheses();
            Write(_field.ConstantValue);
            WriteCloseParentheses();

            WriteComments(_field, false, cancellationToken);
        }
    }
}
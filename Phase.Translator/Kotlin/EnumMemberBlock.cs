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

            Write("@JvmStatic");
            WriteNewLine();
            Write("public val ", Emitter.GetFieldName(_field), " = ");
            Write(Emitter.GetTypeName(_field.ContainingType, true, true, false));
            WriteOpenParentheses();
            Write(_field.ConstantValue);
            WriteCloseParentheses();
            WriteSemiColon(true);
            WriteComments(_field, false, cancellationToken);
        }
    }
}
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Cpp
{
    class EnumMemberBlock : AbstractCppEmitterBlock
    {
        private readonly IFieldSymbol _enumMember;

        public EnumMemberBlock(CppEmitterContext emitterContext, IFieldSymbol enumMember)
        {
            _enumMember = enumMember;
            Init(emitterContext);
        }

        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteComments(_enumMember, true, cancellationToken);

            Write(EmitterContext.GetSymbolName(_enumMember));
            Write(" = ");
            Write(_enumMember.ConstantValue);

            WriteComments(_enumMember, false, cancellationToken);
        }
    }
}

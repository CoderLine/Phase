using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class MemberBindingExpressionBlock : AutoCastBlockBase<MemberBindingExpressionSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            var member = Emitter.GetSymbolInfo(Node);
            if (member.Symbol == null)
            {
                WriteDot();
                Write(Node.Name.Identifier);
            }
            else
            {
                WriteDot();
                Write(EmitterContext.GetSymbolName(member.Symbol));
            }
            return AutoCastMode.Default;
        }
    }
}
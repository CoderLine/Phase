using System.Threading;
using Haxe;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    public class ParenthesizedExpressionBlock : AutoCastBlockBase<ParenthesizedExpressionSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteOpenParentheses();
            var block = EmitTree(Node.Expression, cancellationToken);
            WriteCloseParentheses();
            if (block is IAutoCastBlock)
            {
                return AutoCastMode.SkipCast;
            }
            return AutoCastMode.Default;
        }
    }
}

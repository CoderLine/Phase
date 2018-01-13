using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class ParenthesizedExpressionBlock : AbstractHaxeScriptEmitterBlock<ParenthesizedExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteOpenParentheses();
            EmitTree(Node.Expression, cancellationToken);
            WriteCloseParentheses();
        }
    }
}

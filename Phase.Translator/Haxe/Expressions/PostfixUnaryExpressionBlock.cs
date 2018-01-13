using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class PostfixUnaryExpressionBlock : AbstractHaxeScriptEmitterBlock<PostfixUnaryExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            EmitTree(Node.Operand, cancellationToken);
            Write(Node.OperatorToken.Text);
        }
    }
}
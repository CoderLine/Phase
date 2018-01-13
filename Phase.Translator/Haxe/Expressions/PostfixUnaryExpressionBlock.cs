using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class PostfixUnaryExpressionBlock : AbstractHaxeScriptEmitterBlock<PostfixUnaryExpressionSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            await EmitTreeAsync(Node.Operand, cancellationToken);
            Write(Node.OperatorToken.Text);
        }
    }
}
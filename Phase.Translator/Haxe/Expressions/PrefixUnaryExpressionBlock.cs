using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class PrefixUnaryExpressionBlock : AbstractHaxeScriptEmitterBlock<PrefixUnaryExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            Write(Node.OperatorToken.Text);
            EmitTree(Node.Operand, cancellationToken);
        }
    }
}
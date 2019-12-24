using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript.Expressions
{
    public class PrefixUnaryExpressionBlock : AbstractTypeScriptEmitterBlock<PrefixUnaryExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            Write(Node.OperatorToken.Text);
            EmitTree(Node.Operand, cancellationToken);
        }
    }
}
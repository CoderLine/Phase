using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript.Expressions
{
    public class PostfixUnaryExpressionBlock : AbstractTypeScriptEmitterBlock<PostfixUnaryExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            EmitTree(Node.Operand, cancellationToken);
            switch (Node.Kind())
            {
                case SyntaxKind.SuppressNullableWarningExpression:
                    break;
                default:
                    Write(Node.OperatorToken.Text);
                    break;
            }
        }
    }
}
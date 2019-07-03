using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    public class PostfixUnaryExpressionBlock : AbstractKotlinEmitterBlock<PostfixUnaryExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            EmitTree(Node.Operand, cancellationToken);
            switch (Node.Kind())
            {
                case SyntaxKind.SuppressNullableWarningExpression:
                    Write("!!");
                    break;
                default:
                    Write(Node.OperatorToken.Text);
                    break;
            }
        }
    }
}
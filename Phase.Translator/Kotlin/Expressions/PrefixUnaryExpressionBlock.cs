using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    public class PrefixUnaryExpressionBlock : AutoCastBlockBase<PrefixUnaryExpressionSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Node.Kind() == SyntaxKind.BitwiseNotExpression)
            {
                EmitTree(Node.Operand, cancellationToken);
                Write(".inv()");
            }
            else
            {
                Write(Node.OperatorToken.Text);
                EmitTree(Node.Operand, cancellationToken);
            }
            return AutoCastMode.AddParenthesis;
        }
    }
}
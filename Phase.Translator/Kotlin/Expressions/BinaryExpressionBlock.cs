using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    public class BinaryExpressionBlock : AutoCastBlockBase<BinaryExpressionSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (Node.Kind())
            {
                case SyntaxKind.AddExpression:
                    DoEmit("+", cancellationToken);
                    break;
                case SyntaxKind.SubtractExpression:
                    DoEmit("-", cancellationToken);
                    break;
                case SyntaxKind.MultiplyExpression:
                    DoEmit("*", cancellationToken);
                    break;
                case SyntaxKind.DivideExpression:
                    DoEmit("/", cancellationToken);
                    break;
                case SyntaxKind.ModuloExpression:
                    DoEmit("%", cancellationToken);
                    break;
                case SyntaxKind.LeftShiftExpression:
                    DoEmit("shl", cancellationToken);
                    break;
                case SyntaxKind.RightShiftExpression:
                    DoEmit("shr", cancellationToken);
                    break;
                case SyntaxKind.LogicalOrExpression:
                    DoEmit("||", cancellationToken);
                    break;
                case SyntaxKind.LogicalAndExpression:
                    DoEmit("&&", cancellationToken);
                    break;
                case SyntaxKind.BitwiseOrExpression:
                    DoEmit("or", cancellationToken);
                    break;
                case SyntaxKind.BitwiseAndExpression:
                    DoEmit("and", cancellationToken);
                    break;
                case SyntaxKind.ExclusiveOrExpression:
                    DoEmit("xor", cancellationToken);
                    break;
                case SyntaxKind.EqualsExpression:
                    DoEmit("==", cancellationToken);
                    break;
                case SyntaxKind.NotEqualsExpression:
                    DoEmit("!=", cancellationToken);
                    break;
                case SyntaxKind.LessThanExpression:
                    DoEmit("<", cancellationToken);
                    break;
                case SyntaxKind.LessThanOrEqualExpression:
                    DoEmit("<=", cancellationToken);
                    break;
                case SyntaxKind.GreaterThanExpression:
                    DoEmit(">", cancellationToken);
                    break;
                case SyntaxKind.GreaterThanOrEqualExpression:
                    DoEmit(">=", cancellationToken);
                    break;
                case SyntaxKind.AsExpression:
                    EmitTree(Node.Left, cancellationToken);
                    Write(" as ");
                    EmitTree(Node.Right, cancellationToken);
                    break;
                case SyntaxKind.IsExpression:
                    EmitTree(Node.Left, cancellationToken);
                    Write(" is ");
                    EmitTree(Node.Right, cancellationToken);
                    break;
                case SyntaxKind.CoalesceExpression:
                    EmitTree(Node.Left, cancellationToken);
                    Write(" ?: ");
                    EmitTree(Node.Right, cancellationToken);
                    break;
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                    DoEmit(".", cancellationToken);
                    break;
                default:
                    throw new Exception("unexpected Type given");
            }

            return AutoCastMode.AddParenthesis;
        }

        protected void DoEmit(string op, CancellationToken cancellationToken = default(CancellationToken))
        {
            EmitTree(Node.Left, cancellationToken);
            Write(" ");
            Write(op);
            Write(" ");
            EmitTree(Node.Right, cancellationToken);
        }
    }
}
using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
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
                    DoEmit("<<", cancellationToken);
                    break;
                case SyntaxKind.RightShiftExpression:
                    DoEmit(">>", cancellationToken);
                    break;
                case SyntaxKind.LogicalOrExpression:
                    DoEmit("||", cancellationToken);
                    break;
                case SyntaxKind.LogicalAndExpression:
                    DoEmit("&&", cancellationToken);
                    break;
                case SyntaxKind.BitwiseOrExpression:
                    DoEmit("|", cancellationToken);
                    break;
                case SyntaxKind.BitwiseAndExpression:
                    DoEmit("&", cancellationToken);
                    break;
                case SyntaxKind.ExclusiveOrExpression:
                    DoEmit("^", cancellationToken);
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
                    WriteOpenParentheses();

                    Write("Phase::As<");
                    EmitTree(Node.Right, cancellationToken);
                    Write(">");
                    WriteOpenParentheses();
                    EmitTree(Node.Left, cancellationToken);
                    WriteCloseParentheses();

                    break;
                case SyntaxKind.IsExpression:
                    Write("Phase::Is<");
                    EmitTree(Node.Right, cancellationToken);
                    Write(">(");
                    EmitTree(Node.Left, cancellationToken);
                    Write(")");
                    break;
                case SyntaxKind.CoalesceExpression:
                    // TODO: this way the left expression is executed twice, 
                    EmitTree(Node.Left, cancellationToken);
                    Write(" ? ");
                    EmitTree(Node.Left, cancellationToken);
                    Write(" : ");
                    EmitTree(Node.Right, cancellationToken);
                    break;
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                    DoEmit("->", cancellationToken);
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
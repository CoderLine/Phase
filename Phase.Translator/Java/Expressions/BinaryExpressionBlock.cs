using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Expressions
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

                    Write("system.Phase.as");
                    Write("(");
                    EmitTree(Node.Right, cancellationToken);
                    Write(".class");
                    WriteComma();
                    EmitTree(Node.Left, cancellationToken);
                    Write(")");

                    break;
                case SyntaxKind.IsExpression:
                    Write("system.Phase.is");
                    Write("(");
                    EmitTree(Node.Right, cancellationToken);
                    Write(".class");
                    WriteComma();
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
                    DoEmit(".", cancellationToken);
                    break;
                default:
                    throw new Exception("unexpected Type given");
            }

            return AutoCastMode.AddParenthesis;
        }

        protected void DoEmit(string op, CancellationToken cancellationToken = default(CancellationToken))
        {
            var leftType = Emitter.GetTypeInfo(Node.Left);
            var rightType = Emitter.GetTypeInfo(Node.Right);

            var resultType = Emitter.GetTypeInfo(Node);

            var needsToEnum = resultType.Type?.TypeKind == TypeKind.Enum &&
                               (resultType.ConvertedType == null || resultType.ConvertedType.TypeKind == TypeKind.Enum);

            if (needsToEnum)
            {
                var targetType = resultType.ConvertedType ?? resultType.Type;
                WriteType(targetType);
                Write(".fromValue(");
            }

            EmitTree(Node.Left, cancellationToken);
            if (leftType.Type?.TypeKind == TypeKind.Enum)
            {
                Write(".getValue()");
            }

            Write(" ");
            Write(op);
            Write(" ");

            EmitTree(Node.Right, cancellationToken);
            if (rightType.Type?.TypeKind == TypeKind.Enum)
            {
                Write(".getValue()");
            }

            if (needsToEnum)
            {
                Write(")");
            }
        }
    }
}
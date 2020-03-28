using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript.Expressions
{
    public class BinaryExpressionBlock : AutoCastBlockBase<BinaryExpressionSyntax>
    {
        private ITypeSymbol _rightType;
        private ITypeSymbol _leftType;

        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            _leftType = Emitter.GetTypeInfo(Node.Left).Type;
            _rightType = Emitter.GetTypeInfo(Node.Right).Type;

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
                    // integer division? 
                    // if (IsNumberLiteralOrInlinedConst(Node.Left) && IsNumberLiteralOrInlinedConst(Node.Right))
                    {
                        var leftIsInt = false;
                        var rightIsInt = false;
                        switch (_leftType.SpecialType)
                        {
                            case SpecialType.System_Char:
                            case SpecialType.System_SByte:
                            case SpecialType.System_Byte:
                            case SpecialType.System_Int16:
                            case SpecialType.System_UInt16:
                            case SpecialType.System_Int32:
                            case SpecialType.System_UInt32:
                            case SpecialType.System_Int64:
                            case SpecialType.System_UInt64:
                                leftIsInt = true;
                                break;
                        }
                        if (Emitter.GetTypeName(_leftType) == "Int")
                        {
                            leftIsInt = true;
                        }

                        switch (_rightType.SpecialType)
                        {
                            case SpecialType.System_Char:
                            case SpecialType.System_SByte:
                            case SpecialType.System_Byte:
                            case SpecialType.System_Int16:
                            case SpecialType.System_UInt16:
                            case SpecialType.System_Int32:
                            case SpecialType.System_UInt32:
                            case SpecialType.System_Int64:
                            case SpecialType.System_UInt64:
                                rightIsInt = true;
                                break;
                        }
                        if (Emitter.GetTypeName(_rightType) == "Int")
                        {
                            rightIsInt = true;
                        }

                        if (leftIsInt && rightIsInt)
                        {
                            WriteOpenParentheses();
                            WriteOpenParentheses();
                            DoEmit("/", cancellationToken);
                            WriteCloseParentheses();
                            Write(" | 0");
                            WriteCloseParentheses();
                        }
                        else
                        {
                            DoEmit("/", cancellationToken);
                        }
                    }
                    // else
                    // {
                    //     DoEmit("/", cancellationToken);
                    // }
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

                    Write("(");
                    EmitTree(Node.Left, cancellationToken);
                    Write(" instanceof ");
                    EmitTree(Node.Right, cancellationToken);
                    Write(")");

                    Write("?");

                    EmitTree(Node.Left, cancellationToken);

                    Write(": null");

                    WriteCloseParentheses();
                    break;
                case SyntaxKind.IsExpression:
                    Write("(");
                    EmitTree(Node.Left, cancellationToken);
                    Write(" instanceof ");
                    EmitTree(Node.Right, cancellationToken);
                    Write(")");
                    break;
                case SyntaxKind.CoalesceExpression:
                    EmitTree(Node.Left, cancellationToken);
                    Write(" ?? ");
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

        private bool IsNumberLiteralOrInlinedConst(ExpressionSyntax node)
        {
            if (node.Kind() == SyntaxKind.NumericLiteralExpression)
            {
                return true;
            }

            if (node.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                var symbol = Emitter.GetSymbolInfo(node);
                if (symbol.Symbol != null && symbol.Symbol is IFieldSymbol field && field.IsConst &&
                    field.DeclaringSyntaxReferences.Length == 0)
                {
                    return true;
                }
            }

            return false;
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
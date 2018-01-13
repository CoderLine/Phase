using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class BinaryExpressionBlock : AbstractHaxeScriptEmitterBlock<BinaryExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
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

                    // integer division? 
                    var left = Emitter.GetTypeInfo(Node.Left);
                    var right = Emitter.GetTypeInfo(Node.Right);

                    var leftIsInt = false;
                    var rightIsInt = false;
                    switch (left.Type.SpecialType)
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
                    if (Emitter.GetTypeName(left.Type) == "Int")
                    {
                        leftIsInt = true;
                    }

                    switch (right.Type.SpecialType)
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
                    if (Emitter.GetTypeName(right.Type) == "Int")
                    {
                        rightIsInt = true;
                    }

                    if (leftIsInt && rightIsInt)
                    {
                        Write("Std.int");
                        WriteOpenParentheses();
                        DoEmit("/", cancellationToken);
                        WriteCloseParentheses();
                    }
                    else
                    {
                        DoEmit("/", cancellationToken);
                    }


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
                    Write(PhaseConstants.Phase, ".As(");
                    EmitTree(Node.Left, cancellationToken);
                    Write(",");
                    EmitTree(Node.Right, cancellationToken);
                    Write(")");
                    break;
                case SyntaxKind.IsExpression:
                    Write("Std.is(");
                    EmitTree(Node.Left, cancellationToken);
                    Write(",");
                    EmitTree(Node.Right, cancellationToken);
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
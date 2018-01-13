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
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (Node.Kind())
            {
                case SyntaxKind.AddExpression:
                    await DoEmitAsync("+", cancellationToken);
                    break;
                case SyntaxKind.SubtractExpression:
                    await DoEmitAsync("-", cancellationToken);
                    break;
                case SyntaxKind.MultiplyExpression:
                    await DoEmitAsync("*", cancellationToken);
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
                        await DoEmitAsync("/", cancellationToken);
                        WriteCloseParentheses();
                    }
                    else
                    {
                        await DoEmitAsync("/", cancellationToken);
                    }


                    break;
                case SyntaxKind.ModuloExpression:
                    await DoEmitAsync("%", cancellationToken);
                    break;
                case SyntaxKind.LeftShiftExpression:
                    await DoEmitAsync("<<", cancellationToken);
                    break;
                case SyntaxKind.RightShiftExpression:
                    await DoEmitAsync(">>", cancellationToken);
                    break;
                case SyntaxKind.LogicalOrExpression:
                    await DoEmitAsync("||", cancellationToken);
                    break;
                case SyntaxKind.LogicalAndExpression:
                    await DoEmitAsync("&&", cancellationToken);
                    break;
                case SyntaxKind.BitwiseOrExpression:
                    await DoEmitAsync("|", cancellationToken);
                    break;
                case SyntaxKind.BitwiseAndExpression:
                    await DoEmitAsync("&", cancellationToken);
                    break;
                case SyntaxKind.ExclusiveOrExpression:
                    await DoEmitAsync("^", cancellationToken);
                    break;
                case SyntaxKind.EqualsExpression:
                    await DoEmitAsync("==", cancellationToken);
                    break;
                case SyntaxKind.NotEqualsExpression:
                    await DoEmitAsync("!=", cancellationToken);
                    break;
                case SyntaxKind.LessThanExpression:
                    await DoEmitAsync("<", cancellationToken);
                    break;
                case SyntaxKind.LessThanOrEqualExpression:
                    await DoEmitAsync("<=", cancellationToken);
                    break;
                case SyntaxKind.GreaterThanExpression:
                    await DoEmitAsync(">", cancellationToken);
                    break;
                case SyntaxKind.GreaterThanOrEqualExpression:
                    await DoEmitAsync(">=", cancellationToken);
                    break;
                case SyntaxKind.AsExpression:
                    Write(PhaseConstants.Phase, ".As(");
                    await EmitTreeAsync(Node.Left, cancellationToken);
                    Write(",");
                    await EmitTreeAsync(Node.Right, cancellationToken);
                    Write(")");
                    break;
                case SyntaxKind.IsExpression:
                    Write("Std.is(");
                    await EmitTreeAsync(Node.Left, cancellationToken);
                    Write(",");
                    await EmitTreeAsync(Node.Right, cancellationToken);
                    Write(")");
                    break;
                case SyntaxKind.CoalesceExpression:
                    // TODO: this way the left expression is executed twice, 
                    await EmitTreeAsync(Node.Left, cancellationToken);
                    Write(" ? ");
                    await EmitTreeAsync(Node.Left, cancellationToken);
                    Write(" : ");
                    await EmitTreeAsync(Node.Right, cancellationToken);
                    break;
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                    await DoEmitAsync(".", cancellationToken);
                    break;
                default:
                    throw new Exception("unexpected Type given");
            }
        }

        protected async Task DoEmitAsync(string op, CancellationToken cancellationToken = default(CancellationToken))
        {
            await EmitTreeAsync(Node.Left, cancellationToken);
            Write(" ");
            Write(op);
            Write(" ");
            await EmitTreeAsync(Node.Right, cancellationToken);
        }
    }
}
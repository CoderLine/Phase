using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    public class AssignmentExpressionBlock : AbstractKotlinEmitterBlock<AssignmentExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            var leftSymbol = Emitter.GetSymbolInfo(Node.Left);
            var leftType = Emitter.GetTypeInfo(Node.Left);
            var rightType = Emitter.GetTypeInfo(Node.Right);
            var isStatement = Node.Parent is ExpressionStatementSyntax;

            if (!isStatement)
            {
                Write("run ");
                WriteOpenBrace(true);
            }

            var assignmentHandled = false;
            switch (Node.Kind())
            {
                case SyntaxKind.OrAssignmentExpression:
                    EmitTree(Node.Left, cancellationToken);
                    Write(" = ");
                    EmitTree(Node.Left, cancellationToken);
                    Write(" or (");
                    EmitValue(leftType.Type, rightType.Type, cancellationToken);
                    Write(")");
                    assignmentHandled = true;
                    break;
                case SyntaxKind.AndAssignmentExpression:
                    EmitTree(Node.Left, cancellationToken);
                    Write(" = ");
                    EmitTree(Node.Left, cancellationToken);
                    Write(" and (");
                    EmitValue(leftType.Type, rightType.Type, cancellationToken);
                    Write(")");
                    assignmentHandled = true;
                    break;
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                    EmitTree(Node.Left, cancellationToken);
                    Write(" = ");
                    EmitTree(Node.Left, cancellationToken);
                    Write(" xor (");
                    EmitValue(leftType.Type, rightType.Type, cancellationToken);
                    Write(")");
                    assignmentHandled = true;
                    break;
                case SyntaxKind.LeftShiftAssignmentExpression:
                    EmitTree(Node.Left, cancellationToken);
                    Write(" = ");
                    EmitTree(Node.Left, cancellationToken);
                    Write(" shl (");
                    EmitValue(leftType.Type, rightType.Type, cancellationToken);
                    Write(")");
                    assignmentHandled = true;
                    break;
                case SyntaxKind.RightShiftAssignmentExpression:
                    EmitTree(Node.Left, cancellationToken);
                    Write(" = ");
                    EmitTree(Node.Left, cancellationToken);
                    Write(" shr (");
                    EmitValue(leftType.Type, rightType.Type, cancellationToken);
                    Write(")");
                    assignmentHandled = true;
                    break;
            }

            if (!assignmentHandled)
            {
                if (leftSymbol.Symbol is IEventSymbol evt)
                {
                    var method = Node.Kind() == SyntaxKind.AddAssignmentExpression
                        ? evt.AddMethod
                        : evt.RemoveMethod;

                    if (Node.Left is MemberAccessExpressionSyntax memberAccess)
                    {
                        EmitTree(memberAccess.Expression);
                        Write("!!.");
                    }

                    Write(Emitter.GetMethodName(method));
                    WriteMethodInvocation(method, new List<ParameterInvocationInfo>
                    {
                        new ParameterInvocationInfo(Node.Right)
                    }, cancellationToken: cancellationToken);
                    assignmentHandled = true;
                }
                else if (leftSymbol.Symbol is IPropertySymbol prop && prop.SetMethod != null)
                {
                    var template = Emitter.GetTemplate(prop.SetMethod);
                    if (template != null)
                    {
                        if (template.Variables.TryGetValue("this", out var thisVar))
                        {
                            PushWriter();
                            if (leftSymbol.Symbol.IsStatic)
                            {
                                Write(Emitter.GetTypeName(leftSymbol.Symbol.ContainingType, false, true));
                            }
                            else
                            {
                                EmitTree(Node.Left, cancellationToken);
                            }

                            thisVar.RawValue = PopWriter();
                        }

                        if (template.Variables.TryGetValue("value", out var variable))
                        {
                            PushWriter();
                            EmitValue(leftType.Type, rightType.Type, cancellationToken);
                            variable.RawValue = PopWriter();
                        }

                        Write(template.ToString());
                        return;
                    }
                }

                if (!assignmentHandled)
                {
                    EmitTree(Node.Left, cancellationToken);
                    Write(Node.OperatorToken.Text);
                    EmitValue(leftType.Type, rightType.Type, cancellationToken);
                }
            }

            if (!isStatement)
            {
                WriteSemiColon();
                EmitTree(Node.Left, cancellationToken);
                WriteCloseBrace(true);
            }
        }

        private void EmitValue(ITypeSymbol leftType, ITypeSymbol rightType, CancellationToken cancellationToken)
        {
            if (Node.Right is LiteralExpressionSyntax)
            {
                EmitTree(Node.Right, cancellationToken);
                return;
            }

            PushWriter();
            var block = EmitTree(Node.Right, cancellationToken);
            var result = PopWriter();

            var mode = block is IAutoCastBlock ? AutoCastMode.SkipCast : AutoCastMode.AddParenthesis;

            WriteWithAutoCast(mode, leftType, rightType, result);
        }
    }
}
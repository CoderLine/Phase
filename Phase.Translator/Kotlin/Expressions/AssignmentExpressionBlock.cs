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

            switch (Node.Kind())
            {
                case SyntaxKind.OrAssignmentExpression:
                    EmitTree(Node.Left, cancellationToken);
                    Write(" = ");
                    EmitTree(Node.Left, cancellationToken);
                    Write(" or ");
                    EmitValue(leftType.Type, rightType.Type, cancellationToken);
                    return;
                case SyntaxKind.AndAssignmentExpression:
                    EmitTree(Node.Left, cancellationToken);
                    Write(" = ");
                    EmitTree(Node.Left, cancellationToken);
                    Write(" and ");
                    EmitValue(leftType.Type, rightType.Type, cancellationToken);
                    return;
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                    EmitTree(Node.Left, cancellationToken);
                    Write(" = ");
                    EmitTree(Node.Left, cancellationToken);
                    Write(" xor ");
                    EmitValue(leftType.Type, rightType.Type, cancellationToken);
                    return;
                case SyntaxKind.LeftShiftAssignmentExpression:
                    EmitTree(Node.Left, cancellationToken);
                    Write(" = ");
                    EmitTree(Node.Left, cancellationToken);
                    Write(" shl ");
                    EmitValue(leftType.Type, rightType.Type, cancellationToken);
                    return;
                case SyntaxKind.RightShiftAssignmentExpression:
                    EmitTree(Node.Left, cancellationToken);
                    Write(" = ");
                    EmitTree(Node.Left, cancellationToken);
                    Write(" shr ");
                    EmitValue(leftType.Type, rightType.Type, cancellationToken);
                    return;
            }

            if (leftSymbol.Symbol is IPropertySymbol prop && prop.SetMethod != null)
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

            EmitTree(Node.Left, cancellationToken);
            Write(Node.OperatorToken.Text);
            EmitValue(leftType.Type, rightType.Type, cancellationToken);
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
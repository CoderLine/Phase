using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    public class PostfixUnaryExpressionBlock : AbstractCppEmitterBlock<PostfixUnaryExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var operandSymbol = Emitter.GetSymbolInfo(Node.Operand, cancellationToken);
            if (operandSymbol.Symbol?.Kind == SymbolKind.Property)
            {
                var op = GetOperator();
                switch (Node.Kind())
                {
                    case SyntaxKind.PostIncrementExpression:
                    case SyntaxKind.PostDecrementExpression:

                        if (!string.IsNullOrEmpty(op))
                        {
                            WriteOpenParentheses();
                            EmitTree(Node.Operand, cancellationToken);
                            var property = ((IPropertySymbol)operandSymbol.Symbol);

                            if (property.IsIndexer)
                            {
                                WriteComma();
                            }

                            if (Node.Operand is ElementAccessExpressionSyntax elementAccess)
                            {
                                EmitTree(elementAccess.Expression, cancellationToken);
                                WriteDot();
                                Write(EmitterContext.GetMethodName(property.GetMethod));
                                WriteOpenParentheses();

                                for (int i = 0; i < elementAccess.ArgumentList.Arguments.Count; i++)
                                {
                                    if (i > 0 && property.IsIndexer)
                                    {
                                        WriteComma();
                                    }
                                    EmitTree(elementAccess.ArgumentList.Arguments[i], cancellationToken);
                                }

                                WriteCloseParentheses();
                            }
                            else if (Node.Operand is IdentifierNameSyntax)
                            {
                                Write(EmitterContext.GetMethodName(property.GetMethod));
                                WriteOpenCloseParentheses();

                            }

                            WriteSpace();
                            Write(op);
                            WriteSpace();
                            Write("1");

                            WriteCloseParentheses();
                            WriteCloseParentheses();
                            return;
                        }

                        break;
                }
            }

            Write(Node.OperatorToken.Text);
            EmitTree(Node.Operand, cancellationToken);
        }

        private string GetOperator()
        {
            var op = "";
            switch (Node.Kind())
            {
                case SyntaxKind.PostIncrementExpression:
                    op = "+";
                    break;
                case SyntaxKind.PostDecrementExpression:
                    op = "-";
                    break;
            }

            return op;
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    public class PostfixUnaryExpressionBlock : AbstractKotlinEmitterBlock<PostfixUnaryExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var x = Emitter.GetTypeInfo(Node.Operand, cancellationToken);
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
                            var isEnum = x.Type?.TypeKind == TypeKind.Enum;

                            EmitTree(Node.Operand, cancellationToken);
                            var property = ((IPropertySymbol)operandSymbol.Symbol);

                            if (isEnum)
                            {
                                WriteType(x.Type);
                                Write(".fromValue(");
                            }

                            if (property.IsIndexer)
                            {
                                WriteComma();
                            }

                            if (Node.Operand is ElementAccessExpressionSyntax elementAccess)
                            {
                                EmitTree(elementAccess.Expression, cancellationToken);
                                WriteDot();
                                Write(Emitter.GetMethodName(property.GetMethod));
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
                                Write(Emitter.GetMethodName(property.GetMethod));
                                WriteOpenCloseParentheses();

                            }
                            if (isEnum)
                            {
                                Write(".getValue()");
                            }

                            WriteSpace();
                            Write(op);
                            WriteSpace();
                            Write("1");

                            WriteCloseParentheses();


                            if (isEnum)
                            {
                                WriteCloseParentheses();
                            }
                            return;
                        }

                        break;
                }
            }

            if (x.Type?.TypeKind == TypeKind.Enum)
            {
                EmitTree(Node.Operand, cancellationToken);
                Write(" = ");
                WriteType(x.Type);
                Write(".fromValue(");

                EmitTree(Node.Operand, cancellationToken);
                Write(".getValue()");

                switch (Node.Kind())
                {
                    case SyntaxKind.PostIncrementExpression:
                        Write("+");
                        break;
                    case SyntaxKind.PostDecrementExpression:
                        Write("-");
                        break;
                }

                Write("1");
                Write(")");
            }
            else
            {
                EmitTree(Node.Operand, cancellationToken);
                Write(Node.OperatorToken.Text);
            }
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
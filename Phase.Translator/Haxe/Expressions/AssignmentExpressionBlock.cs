using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class AssignmentExpressionBlock : AbstractHaxeScriptEmitterBlock<AssignmentExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            var leftSymbol = Emitter.GetSymbolInfo(Node.Left);
            var op = GetOperator();
            if (leftSymbol.Symbol != null && leftSymbol.Symbol.Kind == SymbolKind.Property && ((IPropertySymbol)leftSymbol.Symbol).IsIndexer)
            {
                EmitTree(Node.Left, cancellationToken);

                WriteComma();

                if (!string.IsNullOrEmpty(op))
                {
                    var property = ((IPropertySymbol) leftSymbol.Symbol);
                    EmitTree(Node.Left, cancellationToken);
                    WriteDot();
                    Write(Emitter.GetMethodName(property.GetMethod));
                    WriteOpenCloseParentheses();

                    WriteSpace();
                    Write(op);
                    WriteSpace();
                }

                EmitTree(Node.Right, cancellationToken);

                WriteCloseParentheses();
            }
            else
            {
                EmitTree(Node.Left, cancellationToken);
                Write(" = ");
                if (!string.IsNullOrEmpty(op))
                {
                    EmitTree(Node.Left, cancellationToken);
                    WriteSpace();
                    Write(op);
                    WriteSpace();
                }
                EmitTree(Node.Right, cancellationToken);
            }
        }

        private string GetOperator()
        {
            var op = "";
            switch (Node.Kind())
            {
                case SyntaxKind.OrAssignmentExpression:
                    op = "|";
                    break;
                case SyntaxKind.AndAssignmentExpression:
                    op = "&";
                    break;
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                    op = "^";
                    break;
                case SyntaxKind.LeftShiftAssignmentExpression:
                    op = "<<";
                    break;
                case SyntaxKind.RightShiftAssignmentExpression:
                    op = ">>";
                    break;
                case SyntaxKind.AddAssignmentExpression:
                    op = "+";
                    break;
                case SyntaxKind.SubtractAssignmentExpression:
                    op = "-";
                    break;
                case SyntaxKind.MultiplyAssignmentExpression:
                    op = "*";
                    break;
                case SyntaxKind.DivideAssignmentExpression:
                    op = "/";
                    break;
                case SyntaxKind.ModuloAssignmentExpression:
                    op = "%";
                    break;
            }

            return op;
        }
    }
}
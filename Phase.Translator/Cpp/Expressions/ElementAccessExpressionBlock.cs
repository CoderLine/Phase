using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    class ElementAccessExpressionBlock : AbstractCppEmitterBlock<ElementAccessExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            EmitTree(Node.Expression, cancellationToken);

            var symbol = Emitter.GetSymbolInfo(Node);
            if (symbol.Symbol != null && symbol.Symbol.Kind == SymbolKind.Property
                && !Emitter.IsNativeIndexer(symbol.Symbol)
            )                
            {
                var property = ((IPropertySymbol)symbol.Symbol);
                Write("->");

                var writeCloseParenthesis = false;
                switch (Node.Parent.Kind())
                {
                    case SyntaxKind.AddAssignmentExpression:
                    case SyntaxKind.AndAssignmentExpression:
                    case SyntaxKind.DivideAssignmentExpression:
                    case SyntaxKind.ExclusiveOrAssignmentExpression:
                    case SyntaxKind.LeftShiftAssignmentExpression:
                    case SyntaxKind.ModuloAssignmentExpression:
                    case SyntaxKind.MultiplyAssignmentExpression:
                    case SyntaxKind.OrAssignmentExpression:
                    case SyntaxKind.RightShiftAssignmentExpression:
                    case SyntaxKind.SimpleAssignmentExpression:
                    case SyntaxKind.SubtractAssignmentExpression:
                        if (((AssignmentExpressionSyntax) Node.Parent).Left == Node)
                        {
                            Write(Emitter.GetMethodName(property.SetMethod));
                            WriteOpenParentheses();
                        }
                        else
                        {
                            Write(Emitter.GetMethodName(property.GetMethod));
                            WriteOpenParentheses();
                            writeCloseParenthesis = true;
                        }
                        break;
                    default:
                        Write(Emitter.GetMethodName(property.GetMethod));
                        WriteOpenParentheses();
                        writeCloseParenthesis = true; 
                        break;
                }

                for (int i = 0; i < Node.ArgumentList.Arguments.Count; i++)
                {
                    if (i > 0)
                    {
                        WriteComma();
                    }
                    EmitTree(Node.ArgumentList.Arguments[i], cancellationToken);
                }

                if (writeCloseParenthesis)
                {
                    WriteCloseParentheses();
                }
            }
            else
            {
                for (int i = 0; i < Node.ArgumentList.Arguments.Count; i++)
                {
                    WriteOpenBracket();
                    EmitTree(Node.ArgumentList.Arguments[i], cancellationToken);
                    WriteCloseBracket();
                }
            }
        }
    }
}

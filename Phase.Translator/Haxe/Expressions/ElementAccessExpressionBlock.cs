using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    class ElementAccessExpressionBlock : AbstractHaxeScriptEmitterBlock<ElementAccessExpressionSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var type = Emitter.GetTypeInfo(Node, cancellationToken);
            if (type.Type.TypeKind == TypeKind.Dynamic)
            {
                Write("untyped ");
            }

            await EmitTreeAsync(Node.Expression, cancellationToken);

            var symbol = Emitter.GetSymbolInfo(Node);
            if (symbol.Symbol != null && symbol.Symbol.Kind == SymbolKind.Property &&
                ((IPropertySymbol)symbol.Symbol).IsIndexer
                && !Emitter.IsNativeIndexer(symbol.Symbol)
            )                
            {
                var property = ((IPropertySymbol)symbol.Symbol);
                WriteDot();

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
                    await EmitTreeAsync(Node.ArgumentList.Arguments[i], cancellationToken);
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
                    await EmitTreeAsync(Node.ArgumentList.Arguments[i], cancellationToken);
                    WriteCloseBracket();
                }
               
            }

        }
    }
}

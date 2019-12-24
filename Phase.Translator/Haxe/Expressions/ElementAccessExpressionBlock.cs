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
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var type = Emitter.GetTypeInfo(Node, cancellationToken);
            if (type.Type.TypeKind == TypeKind.Dynamic)
            {
                Write("untyped ");
            }

            EmitTree(Node.Expression, cancellationToken);

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
                            Write(EmitterContext.GetMethodName(property.SetMethod));
                            WriteOpenParentheses();
                        }
                        else
                        {
                            Write(EmitterContext.GetMethodName(property.GetMethod));
                            WriteOpenParentheses();
                            writeCloseParenthesis = true;
                        }
                        break;
                    default:
                        Write(EmitterContext.GetMethodName(property.GetMethod));
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

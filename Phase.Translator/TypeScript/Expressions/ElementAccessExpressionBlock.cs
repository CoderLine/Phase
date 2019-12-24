using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Translator.Utils;

namespace Phase.Translator.TypeScript.Expressions
{
    class ElementAccessExpressionBlock : AbstractTypeScriptEmitterBlock<ElementAccessExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var symbol = Emitter.GetSymbolInfo(Node);
            if (symbol.Symbol != null)
            {
                CodeTemplate template = null;
                IPropertySymbol indexer = null;
                if (symbol.Symbol is IPropertySymbol property && property.GetMethod != null)
                {
                    template = Emitter.GetTemplate(property);
                    if (template == null)
                    {
                        template = Emitter.GetTemplate(property.GetMethod);
                    }

                    if (property.IsIndexer)
                    {
                        indexer = property;
                    }
                }
                else
                {
                    template = Emitter.GetTemplate(symbol.Symbol);
                }

                if (template != null)
                {
                    if (template.Variables.TryGetValue("this", out var thisVar))
                    {
                        PushWriter();
                        if (symbol.Symbol.IsStatic)
                        {
                            Write(Emitter.GetTypeName(symbol.Symbol.ContainingType, false, true));
                        }
                        else
                        {
                            EmitTree(Node.Expression, cancellationToken);
                        }

                        thisVar.RawValue = PopWriter();
                    }

                    if (indexer != null)
                    {
                        var arguments = Node.ArgumentList.Arguments.Select(a => new ParameterInvocationInfo(a)).ToList();
                        var invocation = BuildMethodInvocation(indexer.Parameters, arguments);
                        ApplyExpressions(template,indexer.Parameters, invocation, cancellationToken);
                    }


                    Write(template.ToString());
                    return;
                }
            }
            
            EmitTree(Node.Expression, cancellationToken);
            
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

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Translator.Utils;

namespace Phase.Translator.Java.Expressions
{
    class ElementAccessExpressionBlock : AbstractJavaEmitterBlock<ElementAccessExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var symbol = Emitter.GetSymbolInfo(Node);
            if (symbol.Symbol != null && symbol.Symbol.Kind == SymbolKind.Property
                && !Emitter.IsNativeIndexer(symbol.Symbol)
            )                
            {
                var property = ((IPropertySymbol)symbol.Symbol);

                CodeTemplate template;
                if (property.GetMethod != null)
                {
                    template = Emitter.GetTemplate(property.GetMethod);
                }
                else
                {
                    template = Emitter.GetTemplate(property);
                }

                if (template != null)
                {
                    if (template.Variables.TryGetValue("this", out var thisVar))
                    {
                        PushWriter();
                        if (property.IsStatic)
                        {
                            Write(Emitter.GetTypeName(property.ContainingType, false, true));
                        }
                        else
                        {
                            EmitTree(Node.Expression, cancellationToken);
                        }

                        thisVar.RawValue = PopWriter();
                    }

                    for (int i = 0; i < property.Parameters.Length; i++)
                    {
                        IParameterSymbol param = property.Parameters[i];
                        if (template.Variables.TryGetValue(param.Name, out var variable))
                        {
                            PushWriter();
                            EmitTree(Node.ArgumentList.Arguments[i], cancellationToken);
                            var paramOutput = PopWriter();
                            variable.RawValue = paramOutput;
                        }
                    }

                    Write(template.ToString());
                    return;
                }

                EmitTree(Node.Expression, cancellationToken);
                Write(".");

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
                EmitTree(Node.Expression, cancellationToken);
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

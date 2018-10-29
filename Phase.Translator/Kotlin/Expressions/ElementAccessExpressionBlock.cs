using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Translator.Utils;

namespace Phase.Translator.Kotlin.Expressions
{
    class ElementAccessExpressionBlock : AutoCastBlockBase<ElementAccessExpressionSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = new CancellationToken())
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
                    return AutoCastMode.Default;
                }
            }
            EmitTree(Node.Expression, cancellationToken);

            if (Node.ArgumentList.Arguments.Count > 0)
            {
                foreach (var t in Node.ArgumentList.Arguments)
                {
                    Write("!!");
                    WriteOpenBracket();
                    EmitTree(t, cancellationToken);
                    WriteCloseBracket();
                }
            }

            return AutoCastMode.Default;
        }
    }
}

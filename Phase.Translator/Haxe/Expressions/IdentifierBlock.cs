using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class IdentifierBlock : AbstractHaxeScriptEmitterBlock<IdentifierNameSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var resolve = Emitter.GetSymbolInfo(Node);
            if (resolve.Symbol == null)
            {
                Write(Node.Identifier.Text);
            }
            else
            {
                if (resolve.Symbol is ITypeSymbol)
                {
                    WriteType((ITypeSymbol)resolve.Symbol);
                }
                else
                {
                    if (resolve.Symbol.IsStatic &&
                        !resolve.Symbol.ContainingType.Equals(EmitterContext.CurrentType.TypeSymbol))
                    {
                        WriteType(resolve.Symbol.ContainingType);
                        WriteDot();
                    }
                    Write(resolve.Symbol.Name);
                }

                if (!EmitterContext.IsMethodInvocation && (
                    (resolve.Symbol.Kind == SymbolKind.Local && Emitter.IsRefVariable((ILocalSymbol)resolve.Symbol)) ||
                    (resolve.Symbol.Kind == SymbolKind.Parameter && ((IParameterSymbol)resolve.Symbol).RefKind != RefKind.None)
                ))
                {
                    WriteDot();
                    Write("Value");
                }
            }
        }
    }
}
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class IdentifierBlock : AutoCastBlockBase<IdentifierNameSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            var resolve = Emitter.GetSymbolInfo(Node);
            if (resolve.Symbol == null)
            {
                Write(Node.Identifier.Text);
            }
            else
            {
                if (resolve.Symbol != null
                    && resolve.Symbol is IFieldSymbol constField
                    && constField.ContainingType.TypeKind != TypeKind.Enum
                    && constField.IsConst
                    && (constField.DeclaringSyntaxReferences.Length == 0 || EmitterContext.IsCaseLabel))
                {
                    return WriteConstant(constField);
                }
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

                    Write(Emitter.GetSymbolName(resolve.Symbol));
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

            return AutoCastMode.Default;
        }
    }
}
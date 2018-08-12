using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    public class IdentifierBlock : AutoCastBlockBase<IdentifierNameSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(
            CancellationToken cancellationToken = default(CancellationToken))
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
                    Write(Emitter.GetTypeName((ITypeSymbol) resolve.Symbol, false, false));
                }
                else
                {
                    if (resolve.Symbol.IsStatic &&
                        !resolve.Symbol.ContainingType.Equals(EmitterContext.CurrentType.TypeSymbol))
                    {
                        Write(Emitter.GetTypeName(resolve.Symbol.ContainingType, false, false));
                        Write(".");
                    }

                    Write(Emitter.GetSymbolName(resolve.Symbol));
                }
            }

            return AutoCastMode.Default;
        }
    }
}
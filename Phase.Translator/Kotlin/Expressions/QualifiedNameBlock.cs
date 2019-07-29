using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Translator.Utils;

namespace Phase.Translator.Kotlin.Expressions
{
    public class QualifiedNameBlock : AutoCastBlockBase<QualifiedNameSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            var member = Emitter.GetSymbolInfo(Node);

            var expression = Node.Left;
            var leftHandSide = Emitter.GetSymbolInfo(expression);
            if (member.Symbol != null
                && member.Symbol is IFieldSymbol constField
                && constField.ContainingType.TypeKind != TypeKind.Enum
                && constField.IsConst
                && (constField.DeclaringSyntaxReferences.Length == 0 || EmitterContext.IsCaseLabel))
            {
                return WriteConstant(constField);
            }
            
            if (leftHandSide.Symbol == null)
            {
                EmitTree(expression, cancellationToken);
            }
            else
            {
                var kind = leftHandSide.Symbol.Kind;
                switch (kind)
                {
                    case SymbolKind.NamedType:
                        Write(Emitter.GetTypeName((INamedTypeSymbol)leftHandSide.Symbol, false, false));
                        break;
                    default:
                        EmitTree(expression, cancellationToken);
                        break;
                }
            }


            if (member.Symbol == null)
            {
                Write(".");
                Write(Node.Right.Identifier.Text);
            }
            else
            {
                if (member.Symbol.Name == "Value" && member.Symbol.ContainingType.OriginalDefinition.SpecialType ==
                    SpecialType.System_Nullable_T)
                {
                }
                else
                {
                    Write(".");
                    if (member.Symbol.Kind == SymbolKind.NamedType)
                    {
                        Write(Emitter.GetTypeName((INamedTypeSymbol)member.Symbol, true, true));
                    }
                    else
                    {
                        Write(EmitterContext.GetSymbolName(member.Symbol));
                    }
                }
            }

            return AutoCastMode.Default;
        }
    }
}
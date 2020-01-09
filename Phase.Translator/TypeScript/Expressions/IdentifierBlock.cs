using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Build.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript.Expressions
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
                    && constField.IsConst)
                {
                    return WriteConstant(constField);
                }

                if (resolve.Symbol is ITypeSymbol)
                {
                    WriteType((ITypeSymbol) resolve.Symbol);
                }
                else
                {
                    if (resolve.Symbol.IsStatic)
                    {
                        WriteType(resolve.Symbol.ContainingType);
                        WriteDot();
                    }

                    if (EmitterContext.InitializerCount == 0 && !resolve.Symbol.IsStatic)
                    {
                        switch (resolve.Symbol.Kind)
                        {
                            case SymbolKind.Field:
                            case SymbolKind.Property:
                            case SymbolKind.Method:
                            case SymbolKind.Event:
                                Write("this.");
                                break;
                        }
                    }

                    if (EmitterContext.IsAssignmentLeftHand && resolve.Symbol.Kind == SymbolKind.Property
                                                            && !Emitter.IsAutoProperty((IPropertySymbol) resolve.Symbol)
                                                            && ((IPropertySymbol) resolve.Symbol).SetMethod == null
                    )
                    {
                        var backingField = resolve.Symbol.ContainingType
                            .GetMembers()
                            .OfType<IFieldSymbol>()
                            .FirstOrDefault(f => f.AssociatedSymbol == resolve.Symbol);

                        Write(EmitterContext.GetSymbolName(backingField));
                    }
                    else
                    {
                        Write(EmitterContext.GetSymbolName(resolve.Symbol));
                    }


                    if (EmitterContext.InitializerCount == 0 && !resolve.Symbol.IsStatic)
                    {
                        switch (resolve.Symbol.Kind)
                        {
                            case SymbolKind.Event:
                                switch (Node.Parent)
                                {
                                    case EqualsValueClauseSyntax initializer when initializer.Value == Node:
                                    case AssignmentExpressionSyntax assignment when assignment.Right == Node:
                                        Write("?.invoke");
                                        break;
                                }
                                break;
                        }
                    }
                }
            }

            return AutoCastMode.Default;
        }
    }
}
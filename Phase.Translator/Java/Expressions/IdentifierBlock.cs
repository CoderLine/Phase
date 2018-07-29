using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Expressions
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
                    Write(Emitter.GetTypeName((ITypeSymbol)resolve.Symbol, false, false));
                }
                else
                {
                    if (resolve.Symbol.IsStatic &&
                        !resolve.Symbol.ContainingType.Equals(EmitterContext.CurrentType.TypeSymbol))
                    {
                        Write(Emitter.GetTypeName(resolve.Symbol.ContainingType, false, false));
                        Write(".");
                    }
                    else if (resolve.Symbol != null && resolve.Symbol.Kind == SymbolKind.Property && !Emitter.IsNativeIndexer(resolve.Symbol))
                    {
                        var property = ((IPropertySymbol)resolve.Symbol);
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
                                if (((AssignmentExpressionSyntax)Node.Parent).Left == Node)
                                {
                                    WriteSetterAccess(property);
                                }
                                else
                                {
                                    Write(Emitter.GetMethodName(property.GetMethod));

                                    WriteOpenParentheses();
                                    writeCloseParenthesis = true;
                                }
                                break;
                            case SyntaxKind.PreIncrementExpression:
                            case SyntaxKind.PostIncrementExpression:
                            case SyntaxKind.PreDecrementExpression:
                            case SyntaxKind.PostDecrementExpression:
                                WriteSetterAccess(property);
                                break;
                            default:
                                Write(Emitter.GetMethodName(property.GetMethod));
                                WriteOpenParentheses();
                                writeCloseParenthesis = true;
                                break;
                        }

                        if (writeCloseParenthesis)
                        {
                            WriteCloseParentheses();
                        }

                        return AutoCastMode.Default;
                    }
                    Write(Emitter.GetSymbolName(resolve.Symbol));
                }
            }

            return AutoCastMode.Default;
        }

        private void WriteSetterAccess(IPropertySymbol property)
        {
            if (property.SetMethod == null)
            {
                var backingField = property.ContainingType.GetMembers().OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.AssociatedSymbol.Equals(property));
                if (backingField != null)
                {
                    Write(Emitter.GetFieldName(backingField));
                    Write(" = ");
                }
                else
                {
                    throw new PhaseCompilerException("What kind of set is used here?");
                }
            }
            else
            {
                Write(Emitter.GetMethodName(property.SetMethod));
                WriteOpenParentheses();
            }
        }
    }
}
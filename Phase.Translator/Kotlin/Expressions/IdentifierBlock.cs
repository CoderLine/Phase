using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
                    Write(Emitter.GetTypeName((ITypeSymbol)resolve.Symbol, false, false));
                }
                else if (resolve.Symbol is IMethodSymbol method && Node.Parent.Kind() != SyntaxKind.InvocationExpression)
                {
                    Write("::");
                    Write(EmitterContext.GetMethodName(method));
                    return AutoCastMode.SkipCast;
                }
                else
                {
                    if (resolve.Symbol.IsStatic)
                    {
                        Write(Emitter.GetTypeName(resolve.Symbol.ContainingType, false, false));
                        Write(".");
                    }
                    else if (!resolve.Symbol.IsStatic && (resolve.Symbol.Kind == SymbolKind.Field || resolve.Symbol.Kind == SymbolKind.Property) && EmitterContext.RecursiveObjectCreation == 0)
                    {
                        // write this for fields and properites to prevent clashes with local variables and parameters
                        Write("this.");
                    }


                    if (resolve.Symbol is IEventSymbol evt && Node.Parent.Kind() != SyntaxKind.InvocationExpression)
                    {
                        Write("if (");
                        Write(EmitterContext.GetSymbolName(resolve.Symbol));
                        Write(" != null) ");
                        Write(EmitterContext.GetSymbolName(resolve.Symbol));
                        Write("!!::");
                        Write(EmitterContext.GetMethodName(((INamedTypeSymbol)evt.Type).DelegateInvokeMethod));
                        Write(" else null");
                    }
                    else
                    {
                        Write(EmitterContext.GetSymbolName(resolve.Symbol));
                    }
                }
            }

            return AutoCastMode.Default;
        }
    }
}
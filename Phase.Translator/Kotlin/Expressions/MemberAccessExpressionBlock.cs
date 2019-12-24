using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Translator.Utils;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Phase.Translator.Kotlin.Expressions
{
    public class MemberAccessExpressionBlock : AutoCastBlockBase<MemberAccessExpressionSyntax>
    {
        public bool SkipSemicolonOnStatement { get; set; }

        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            var member = Emitter.GetSymbolInfo(Node);

            if (member.Symbol != null)
            {
                CodeTemplate template;
                if (member.Symbol is IPropertySymbol property && property.GetMethod != null)
                {
                    template = Emitter.GetTemplate(property.GetMethod);
                }
                else
                {
                    template = Emitter.GetTemplate(member.Symbol);
                }

                if (template != null)
                {
                    SkipSemicolonOnStatement = template.SkipSemicolonOnStatements;
                    if (template.Variables.TryGetValue("this", out var thisVar))
                    {
                        PushWriter();
                        if (member.Symbol.IsStatic)
                        {
                            Write(Emitter.GetTypeName(member.Symbol.ContainingType, false, true, false));
                        }
                        else
                        {
                            EmitTree(Node.Expression, cancellationToken);
                        }

                        thisVar.RawValue = PopWriter();
                    }

                    Write(template.ToString());
                    return AutoCastMode.SkipCast;
                }
            }

            var expression = Node.Expression;
            var leftHandSide = Emitter.GetSymbolInfo(expression);
            if (member.Symbol != null
                && member.Symbol is IFieldSymbol constField
                && constField.ContainingType.TypeKind != TypeKind.Enum
                && constField.IsConst
                && (constField.DeclaringSyntaxReferences.Length == 0 || EmitterContext.IsCaseLabel))
            {
                return WriteConstant(constField);
            }
            if (member.Symbol != null
                && member.Symbol is IFieldSymbol enumField
                && enumField.ContainingType.TypeKind == TypeKind.Enum
                && EmitterContext.IsCaseLabel)
            {
                Write(enumField.Name);
                return AutoCastMode.Default;
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
                        Write(Emitter.GetTypeName((INamedTypeSymbol)leftHandSide.Symbol, false, false, false));
                        break;
                    default:
                        EmitTree(expression, cancellationToken);
                        break;
                }
            }


            if (member.Symbol == null)
            {
                Write("!!.");
                Write(Node.Name.Identifier);
            }
            else
            {
                if (member.Symbol.IsStatic)
                {
                    Write(".");
                }
                else
                {
                    Write("!!.");
                }
                Write(Emitter.GetSymbolName(member.Symbol));
            }

            return AutoCastMode.Default;
        }
    }
}
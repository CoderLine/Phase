using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Translator.Utils;

namespace Phase.Translator.TypeScript.Expressions
{
    public class MemberAccessExpressionBlock : AutoCastBlockBase<MemberAccessExpressionSyntax>
    {
        public bool SkipSemicolonOnStatement { get; set; }

        protected override AutoCastMode DoEmitWithoutCast(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var member = Emitter.GetSymbolInfo(Node);

            if (member.Symbol != null)
            {
                CodeTemplate template = null;
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
                            Write(Emitter.GetTypeName(member.Symbol.ContainingType, false, true));
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
                && (constField.ContainingType.SpecialType != SpecialType.System_Single || constField.Name != "NaN")
                && (constField.ContainingType.SpecialType != SpecialType.System_Double || constField.Name != "NaN"))
            {
                return WriteConstant(constField);
            }

            PushWriter();

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
                        Write(Emitter.GetTypeName((INamedTypeSymbol) leftHandSide.Symbol, false, true));
                        EmitterContext.ImportType((INamedTypeSymbol) leftHandSide.Symbol);
                        break;
                    default:
                        EmitTree(expression, cancellationToken);
                        break;
                }
            }

            var leftHandSideOutput = PopWriter();
            Write(leftHandSideOutput);

            if (member.Symbol == null)
            {
                WriteDot();
                Write(Node.Name.Identifier.Text.ToCamelCase());
            }
            else
            {
                if (member.Symbol.Name == "Value" && member.Symbol.ContainingType.OriginalDefinition.SpecialType ==
                    SpecialType.System_Nullable_T)
                {
                }
                else
                {
                    WriteDot();
                    Write(EmitterContext.GetSymbolName(member.Symbol));
                    if (member.Symbol.Kind == SymbolKind.Method)
                    {
                        var parentKind = Node.Parent.Kind();
                        if (parentKind != SyntaxKind.InvocationExpression)
                        {
                            Write(".bind(", leftHandSideOutput, ")");
                        }
                    }
                }
            }

            return AutoCastMode.Default;
        }
    }
}
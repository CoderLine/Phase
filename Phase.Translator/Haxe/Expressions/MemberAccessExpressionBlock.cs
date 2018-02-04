using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Haxe;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Translator.Utils;

namespace Phase.Translator.Haxe.Expressions
{
    public class MemberAccessExpressionBlock : AutoCastBlockBase<MemberAccessExpressionSyntax>
    {
        public bool SkipSemicolonOnStatement { get; set; }

        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
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
            if (member.Symbol != null && member.Symbol is IFieldSymbol constField && constField.IsConst && constField.DeclaringSyntaxReferences.Length == 0)
            {
                switch (constField.Type.SpecialType)
                {
                    case SpecialType.System_Boolean:
                        Write((bool)constField.ConstantValue ? "true" : "false");
                        return AutoCastMode.SkipCast;
                    case SpecialType.System_Char:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt64:
                        Write((int)constField.ConstantValue);
                        return AutoCastMode.SkipCast;
                    case SpecialType.System_Decimal:
                        Write((decimal)constField.ConstantValue);
                        return AutoCastMode.SkipCast;
                    case SpecialType.System_Single:
                        Write((float)constField.ConstantValue);
                        return AutoCastMode.SkipCast;
                    case SpecialType.System_Double:
                        Write((double)constField.ConstantValue);
                        return AutoCastMode.SkipCast;
                    case SpecialType.System_String:
                        Write("\"" + constField.ConstantValue + "\"");
                        return AutoCastMode.SkipCast;
                }
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
                    case SymbolKind.Field:
                        var field = (IFieldSymbol)leftHandSide.Symbol;
                        if (field.IsStatic)
                        {
                            WriteType(field.ContainingType);
                            WriteDot();
                        }
                        Write(Emitter.GetFieldName(field));
                        //}
                        break;
                    case SymbolKind.NamedType:
                        Write(Emitter.GetTypeName((INamedTypeSymbol)leftHandSide.Symbol, false, true));
                        break;
                    default:
                        EmitTree(expression, cancellationToken);
                        break;
                }
            }

            if (member.Symbol == null)
            {
                WriteDot();
                Write(Node.Name.Identifier);
            }
            else
            {
                WriteDot();
                Write(Emitter.GetSymbolName(member.Symbol));
            }

            return AutoCastMode.Default;
        }
    }
}
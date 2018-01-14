using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class MemberAccessExpressionBlock : AbstractHaxeScriptEmitterBlock<MemberAccessExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
        

            var expression = Node.Expression;
            var leftHandSide = Emitter.GetSymbolInfo(expression);
            var member = Emitter.GetSymbolInfo(Node);
            if (member.Symbol != null && member.Symbol is IFieldSymbol constField && constField.IsConst && constField.DeclaringSyntaxReferences.Length == 0)
            {
                switch (constField.Type.SpecialType)
                {
                    case SpecialType.System_Boolean:
                        Write((bool)constField.ConstantValue ? "true" : "false");
                        return;
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
                        return;
                    case SpecialType.System_Decimal:
                        Write((decimal)constField.ConstantValue);
                        return;
                    case SpecialType.System_Single:
                        Write((float)constField.ConstantValue);
                        return;
                    case SpecialType.System_Double:
                        Write((double)constField.ConstantValue);
                        return;
                    case SpecialType.System_String:
                        Write("\"" + constField.ConstantValue + "\"");
                        return;
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
                        Write(Emitter.GetTypeName((INamedTypeSymbol) leftHandSide.Symbol, false, true));
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
                switch (member.Symbol.Kind)
                {
                    case SymbolKind.Method:
                        WriteDot();
                        Write(Emitter.GetMethodName((IMethodSymbol)member.Symbol));
                        break;
                    case SymbolKind.Field:
                        WriteDot();
                        Write(Emitter.GetFieldName((IFieldSymbol)member.Symbol));
                        break;
                    case SymbolKind.Property:
                        WriteDot();
                        Write(Emitter.GetPropertyName((IPropertySymbol)member.Symbol));
                        break;
                    case SymbolKind.Event:
                        WriteDot();
                        Write(Emitter.GetEventName((IEventSymbol)member.Symbol));
                        break;
                    default:
                        WriteDot();
                        Write(Node.Name.Identifier);
                        break;
                }
            }

            var typeInfo = Emitter.GetTypeInfo(Node, cancellationToken);
            // implicit cast
            if (typeInfo.ConvertedType != null && !typeInfo.Type.Equals(typeInfo.ConvertedType))
            {
                switch (typeInfo.ConvertedType.SpecialType)
                {
                    case SpecialType.System_Boolean:
                    case SpecialType.System_Char:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt64:
                        if (Emitter.IsIConvertible(typeInfo.Type))
                        {
                            WriteDot();
                            Write("To" + typeInfo.ConvertedType.Name + "_IFormatProvider");
                            WriteOpenParentheses();
                            Write("null");
                            WriteCloseParentheses();
                        }
                        return;
                }
            }
        }
    }
}
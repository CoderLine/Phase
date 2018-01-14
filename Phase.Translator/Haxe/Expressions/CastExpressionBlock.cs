using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    class CastExpressionBlock : AbstractHaxeScriptEmitterBlock<CastExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var sourceType = Emitter.GetTypeInfo(Node.Expression);
            var targetType = Emitter.GetTypeInfo(Node);

            switch (targetType.Type.SpecialType)
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
                    if (sourceType.Type.Equals(targetType.Type))
                    {
                        EmitTree(Node.Expression, cancellationToken);
                    }
                    else if (Emitter.IsIConvertible(sourceType.Type))
                    {
                        EmitTree(Node.Expression, cancellationToken);
                        WriteDot();
                        Write("To" + targetType.Type.Name + "_IFormatProvider");
                        WriteOpenParentheses();
                        Write("null");
                        WriteCloseParentheses();
                    }
                    else
                    {
                        Write("untyped ");
                        EmitTree(Node.Expression, cancellationToken);
                    }
                    return;
            }

            if (targetType.Type.TypeKind == TypeKind.Delegate)
            {
                EmitTree(Node.Expression, cancellationToken);
            }
            else
            {
                Write("cast");
                WriteOpenParentheses();
                EmitTree(Node.Expression, cancellationToken);
                if (sourceType.Type.SpecialType != SpecialType.System_Object &&
                    targetType.Type != null && targetType.Type.TypeKind != TypeKind.TypeParameter && !IsSpecialArray(targetType.Type))
                {
                    WriteComma();
                    WriteType(targetType.Type);
                }

                WriteCloseParentheses();
            }
        }

        private bool IsSpecialArray(ITypeSymbol type)
        {
            return type.TypeKind == TypeKind.Array &&
                   Emitter.GetSpecialArrayName(((IArrayTypeSymbol)type).ElementType) != null;
        }
    }
}
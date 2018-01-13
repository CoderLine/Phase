using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    class CastExpressionBlock : AbstractHaxeScriptEmitterBlock<CastExpressionSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
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

                    switch (sourceType.Type.SpecialType)
                    {
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                        case SpecialType.System_Decimal:
                            Write("Std.int");
                            WriteOpenParentheses();
                            await EmitTreeAsync(Node.Expression, cancellationToken);
                            WriteCloseParentheses();
                            return;

                        default:
                            Write("cast");
                            WriteOpenParentheses();
                            await EmitTreeAsync(Node.Expression, cancellationToken);
                            WriteCloseParentheses();
                            return;
                    }
            }

            if (targetType.Type.TypeKind == TypeKind.Delegate)
            {
                await EmitTreeAsync(Node.Expression, cancellationToken);
            }
            else
            {
                Write("cast");
                WriteOpenParentheses();
                await EmitTreeAsync(Node.Expression, cancellationToken);
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
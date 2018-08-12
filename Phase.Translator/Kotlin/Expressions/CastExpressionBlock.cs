using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    class CastExpressionBlock : AutoCastBlockBase<CastExpressionSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            var sourceType = Emitter.GetTypeInfo(Node.Expression);
            var targetType = Emitter.GetTypeInfo(Node);

            if (sourceType.Type.TypeKind == TypeKind.Enum)
            {
                EmitTree(Node.Expression, cancellationToken);
                Write(".value.to");
                Write(Emitter.GetTypeName(targetType.Type, true, true, false));
                WriteOpenCloseParentheses();
            }
            else if (targetType.Type.TypeKind == TypeKind.Enum)
            {
                WriteType(targetType.Type);
                Write(".fromValue(");
                EmitTree(Node.Expression, cancellationToken);
                Write(")");
            }
            else
            {
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
                        if (Emitter.IsIConvertible(sourceType.Type))
                        {
                            EmitTree(Node.Expression, cancellationToken);
                            WriteDot();
                            Write("to" + Emitter.GetTypeName(targetType.Type, true, true, false));
                            WriteOpenCloseParentheses();
                            return AutoCastMode.Default;
                        }
                        break;
                }

                Write("(");
                WriteType(targetType.Type);
                Write(")");
                EmitTree(Node.Expression, cancellationToken);
                return AutoCastMode.AddParenthesis;
            }
            return AutoCastMode.Default;
        }
    }
}
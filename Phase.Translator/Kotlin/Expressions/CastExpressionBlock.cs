using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
                Write("(");
                EmitTree(Node.Expression, cancellationToken);
                Write(").value.to");
                Write(Emitter.GetTypeName(targetType.Type, true, true));
                WriteOpenCloseParentheses();
            }
            else if (targetType.Type.TypeKind == TypeKind.Enum)
            {
                WriteType(targetType.Type);
                Write(".fromValue(");
                EmitTree(Node.Expression, cancellationToken);
                Write(".toInt())");
            }
            else
            {
                EmitTree(Node.Expression, cancellationToken);
             
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
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                        if (Emitter.IsIConvertible(sourceType.Type))
                        {
                            WriteDot();
                            Write("to" + Emitter.GetTypeName(targetType.Type, true, true));
                            WriteOpenCloseParentheses();

                            return AutoCastMode.Default;
                        }
                        break;
                }
                Write(" as ");
                WriteType(targetType.Type);

                return AutoCastMode.AddParenthesis;
            }
            return AutoCastMode.Default;
        }
    }
}
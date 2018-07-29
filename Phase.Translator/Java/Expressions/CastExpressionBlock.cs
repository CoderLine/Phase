using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Expressions
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
                WriteType(targetType.Type);
                Write(")");
                EmitTree(Node.Expression, cancellationToken);
                Write(".getValue()");
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
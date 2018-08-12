using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    public class ImplicitArrayCreationExpressionBlock : AbstractKotlinEmitterBlock<ImplicitArrayCreationExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            string arrayFunction;
            var arrayType = (IArrayTypeSymbol)Emitter.GetTypeInfo(Node, cancellationToken).Type;
            var elementType = arrayType.ElementType;
            switch (elementType.SpecialType)
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                    arrayFunction = "byteArrayOf";
                    break;
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                    arrayFunction = "shortArrayOf";
                    break;
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                    arrayFunction = "intArrayOf";
                    break;
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    arrayFunction = "longArrayOf";
                    break;
                case SpecialType.System_Boolean:
                    arrayFunction = "booleanArrayOf";
                    break;
                case SpecialType.System_Char:
                    arrayFunction = "charArrayOf";
                    break;
                case SpecialType.System_Single:
                    arrayFunction = "floatArrayOf";
                    break;
                case SpecialType.System_Decimal:
                case SpecialType.System_Double:
                    arrayFunction = "doubleArrayOf";
                    break;
                default:
                    arrayFunction = "arrayOf";
                    break;
            }

            Write(arrayFunction);
            WriteOpenParentheses();

            for (var i = 0; i < Node.Initializer.Expressions.Count; i++)
            {
                var expression = Node.Initializer.Expressions[i];
                if (i > 0) WriteComma();
                EmitTree(expression, cancellationToken);
            }

            WriteCloseParentheses();
        }
    }
}
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
            var arrayType = (IArrayTypeSymbol)Emitter.GetTypeInfo(Node, cancellationToken).Type;
            var arrayFunction = Emitter.GetArrayCreationFunctionName(arrayType);
           

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
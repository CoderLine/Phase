using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Expressions
{
    public class ImplicitArrayCreationExpressionBlock : AbstractJavaEmitterBlock<ImplicitArrayCreationExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var arrayType = Emitter.GetTypeInfo(Node, cancellationToken);

            WriteNew();
            Write(Emitter.GetTypeName(arrayType.Type, false, true));
            for (int i = 0; i < ((IArrayTypeSymbol)arrayType.Type).Rank; i++)
            {
                Write("[]");
            }

            BeginBlock();

            for (var i = 0; i < Node.Initializer.Expressions.Count; i++)
            {
                var expression = Node.Initializer.Expressions[i];
                if (i > 0) WriteComma();
                EmitTree(expression, cancellationToken);
            }

            EndBlock();
        }
    }
}
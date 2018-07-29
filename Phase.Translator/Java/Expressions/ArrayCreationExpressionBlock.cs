using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Expressions
{
    public class ArrayCreationExpressionBlock : AbstractJavaEmitterBlock<ArrayCreationExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            var arrayType = (IArrayTypeSymbol)Emitter.GetTypeSymbol(Node.Type);

            WriteNew();
            Write(Emitter.GetTypeName(arrayType.ElementType, false, true));

            for (int i = 0; i < Node.Type.RankSpecifiers.Count; i++)
            {
                for (int j = 0; j < Node.Type.RankSpecifiers[i].Sizes.Count; j++)
                {
                    var expr = Node.Type.RankSpecifiers[i].Sizes[j];

                    if (expr.Kind() != SyntaxKind.OmittedArraySizeExpression)
                    {
                        Write("[");
                        EmitTree(Node.Type.RankSpecifiers[i].Sizes[j], cancellationToken);
                        Write("]");
                    }
                    else
                    {
                        Write("[]");
                    }
                }
            }

            if (Node.Initializer != null)
            {
                Write("{");
                for (var i = 0; i < Node.Initializer.Expressions.Count; i++)
                {
                    var expression = Node.Initializer.Expressions[i];
                    if (i > 0) WriteComma();
                    EmitTree(expression, cancellationToken);
                }
                Write("}");
            }
        }
    }
}
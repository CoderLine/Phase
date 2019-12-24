using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript.Expressions
{
    public class ImplicitArrayCreationExpressionBlock : AbstractTypeScriptEmitterBlock<ImplicitArrayCreationExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteOpenBracket();

            for (int i = 0; i < Node.Initializer.Expressions.Count; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                }
                EmitTree(Node.Initializer.Expressions[i], cancellationToken);
            }

            WriteCloseBracket();
        }
    }
}
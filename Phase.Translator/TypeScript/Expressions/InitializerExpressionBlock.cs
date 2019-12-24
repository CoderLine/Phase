using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript.Expressions
{
    public class InitializerExpressionBlock : AbstractTypeScriptEmitterBlock<InitializerExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteOpenBracket();
            EmitterContext.InitializerCount++;

            for (int i = 0; i < Node.Expressions.Count; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                }
                EmitTree(Node.Expressions[i], cancellationToken);
            }

            EmitterContext.InitializerCount--;
            WriteCloseBracket();
        }
    }
}
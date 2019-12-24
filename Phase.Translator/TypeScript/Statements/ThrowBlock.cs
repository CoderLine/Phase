using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Translator.Utils;

namespace Phase.Translator.TypeScript
{
    public class ThrowBlock : CommentedNodeEmitBlock<ThrowStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteThrow();
            if (Node.Expression != null)
            {
                EmitTree(Node.Expression, cancellationToken);
            }
            else
            {
                Write(EmitterContext.CurrentExceptionName.Peek());
            }
            WriteSemiColon(true);
        }
    }
}
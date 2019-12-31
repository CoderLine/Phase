using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Phase.Translator.TypeScript
{
    public class ReturnBlock : CommentedNodeEmitBlock<ReturnStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Node.Expression != null)
            {
                WriteReturn(true);
                EmitTree(Node.Expression, cancellationToken);
            }
            else
            {
                WriteReturn(false);
            }
            WriteSemiColon(true);
            WriteNewLine();
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
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
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Statements
{
    public class CheckedBlock : CommentedNodeEmitBlock<CheckedStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            EmitTree(Node.Block, cancellationToken);
        }
    }
}
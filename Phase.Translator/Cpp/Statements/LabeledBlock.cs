using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Statements
{
    public class LabeledBlock : CommentedNodeEmitBlock<LabeledStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            EmitTree(Node.Statement, cancellationToken);
        }
    }
}
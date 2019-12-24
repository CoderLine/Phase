using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Statements
{
    public class FixedBlock : CommentedNodeEmitBlock<FixedStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            EmitTree(Node.Declaration, cancellationToken);
            WriteSemiColon(true);
            EmitTree(Node.Statement, cancellationToken);
        }
    }
}
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Statements
{
    public class LockBlock : CommentedNodeEmitBlock<LockStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            EmitTree(Node.Expression, cancellationToken);
            WriteSemiColon(true);

            EmitTree(Node.Statement, cancellationToken);
        }
    }
}
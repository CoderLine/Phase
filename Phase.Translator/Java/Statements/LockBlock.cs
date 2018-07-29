using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Statements
{
    public class LockBlock : CommentedNodeEmitBlock<LockStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            Write("synchronized");
            WriteOpenParentheses();
            EmitTree(Node.Expression, cancellationToken);
            WriteCloseParentheses();
            EmitTree(Node.Statement, cancellationToken);
        }
    }
}
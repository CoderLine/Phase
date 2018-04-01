using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Statements
{
    public class ContinueBlock : CommentedNodeEmitBlock<ContinueStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            Write("continue");
            WriteSemiColon(true);
        }
    }
}
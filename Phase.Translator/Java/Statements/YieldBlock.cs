using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Statements
{
    public class YieldBlock : CommentedNodeEmitBlock<YieldStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new PhaseCompilerException("Yield statements are not yet supported");
        }
    }
}
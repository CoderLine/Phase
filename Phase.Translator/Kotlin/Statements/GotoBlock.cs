using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Statements
{
    public class GotoBlock : CommentedNodeEmitBlock<GotoStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteSemiColon(true);
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class EmptyBlock : CommentedNodeEmitBlock<EmptyStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
        }
    }
}
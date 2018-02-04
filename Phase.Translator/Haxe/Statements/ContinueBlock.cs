using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class ContinueBlock : CommentedNodeEmitBlock<ContinueStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var incrementors = EmitterContext.CurrentForIncrementors.Count > 0
                ? EmitterContext.CurrentForIncrementors.Peek()
                : null;

            if (incrementors != null)
            {
                foreach (var incrementor in incrementors)
                {
                    EmitTree(incrementor, cancellationToken);
                    WriteSemiColon(true);
                }
            }

            Write("continue");
            WriteSemiColon(true);
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class ThrowBlock : CommentedNodeEmitBlock<ThrowStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteThrow();
            EmitTree(Node.Expression, cancellationToken);
            WriteSemiColon(true);
        }
    }
}
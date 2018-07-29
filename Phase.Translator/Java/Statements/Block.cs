using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Statements
{
    public class Block : CommentedNodeEmitBlock<BlockSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            BeginBlock();

            foreach (var statementSyntax in Node.Statements)
            {
                EmitTree(statementSyntax, cancellationToken);
            }

            EndBlock();
        }
    }
}

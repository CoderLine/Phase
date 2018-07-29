using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Java
{
    public abstract class CommentedNodeEmitBlock<T> : AbstractJavaEmitterBlock<T> where T : SyntaxNode
    {
        protected override void BeginEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            base.BeginEmit(cancellationToken);
            WriteComments(Node);
        }

        protected override void EndEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            base.EndEmit(cancellationToken);
            WriteComments(Node, false);
        }
    }
}

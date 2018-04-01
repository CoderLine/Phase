using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Statements
{
    public class ThrowBlock : CommentedNodeEmitBlock<ThrowStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteThrow();
            if (Node.Expression != null)
            {
                EmitTree(Node.Expression, cancellationToken);
            }
            else
            {
                Write(EmitterContext.CurrentExceptionName.Peek());
            }
            WriteSemiColon(true);
        }
    }
}
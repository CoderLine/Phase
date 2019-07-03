using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Statements
{
    public class WhileBlock : CommentedNodeEmitBlock<WhileStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            PushWriter();
            EmitTree(Node.Statement, cancellationToken);
            var body = PopWriter();

            if (EmitterContext.LoopNames.TryGetValue(Node, out var name))
            {
                Write(name, "@ ");
                EmitterContext.LoopNames.Remove(Node);
            }

            WriteWhile();
            WriteOpenParentheses();
            EmitTree(Node.Condition, cancellationToken);
            WriteCloseParentheses();
            WriteNewLine();
            Write(body.TrimStart());
        }
    }
}
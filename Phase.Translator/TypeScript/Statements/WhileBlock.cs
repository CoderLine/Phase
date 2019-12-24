using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript
{
    public class WhileBlock : CommentedNodeEmitBlock<WhileStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteWhile();
            WriteOpenParentheses();
            EmitTree(Node.Condition, cancellationToken);
            WriteCloseParentheses();
            WriteNewLine();
            EmitTree(Node.Statement, cancellationToken);
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class DoWhileBlock : CommentedNodeEmitBlock<DoStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteDo();
            EmitTree(Node.Statement, cancellationToken);
            if (Node.Statement.Kind() == SyntaxKind.Block)
            {
                WriteSpace();
            }

            WriteWhile();
            WriteOpenParentheses();
            EmitTree(Node.Condition, cancellationToken);
            WriteCloseParentheses();
            WriteSemiColon(true);
        }
    }
}
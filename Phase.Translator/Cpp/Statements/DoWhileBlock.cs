using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Statements
{
    public class DoWhileBlock : CommentedNodeEmitBlock<DoStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteDo();
            if (Node.Statement.Kind() == SyntaxKind.Block)
            {
                WriteNewLine();
            }
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
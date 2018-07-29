using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Statements
{
    public class IfBlock : CommentedNodeEmitBlock<IfStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteIf();
            WriteOpenParentheses();
            EmitTree(Node.Condition, cancellationToken);
            WriteCloseParentheses();
            WriteNewLine();

            EmitTree(Node.Statement, cancellationToken);

            if (Node.Else != null)
            {
                WriteElse();
                EmitTree(Node.Else.Statement, cancellationToken);
            }
        }
    }
}
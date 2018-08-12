using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Statements
{
    public class BreakBlock : CommentedNodeEmitBlock<BreakStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Node.Parent.Kind() != SyntaxKind.SwitchSection)
            {
                Write("break");
                WriteSemiColon(true);
            }
        }
    }
}
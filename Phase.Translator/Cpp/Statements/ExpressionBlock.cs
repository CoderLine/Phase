using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Translator.Cpp.Expressions;

namespace Phase.Translator.Cpp.Statements
{
    public class ExpressionBlock : CommentedNodeEmitBlock<ExpressionStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var emit = EmitTree(Node.Expression, cancellationToken) as InvocationExpressionBlock;
            if (emit == null || !emit.SkipSemicolonOnStatement)
            {
                WriteSemiColon();
            }
        }

        protected override void EndEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            base.EndEmit(cancellationToken);
            WriteNewLine();
        }
    }
}
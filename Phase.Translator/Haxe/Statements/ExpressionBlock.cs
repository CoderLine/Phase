using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Translator.Haxe.Expressions;

namespace Phase.Translator.Haxe
{
    public class ExpressionBlock : AbstractHaxeScriptEmitterBlock<ExpressionStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var emit = EmitTree(Node.Expression, cancellationToken) as InvocationExpressionBlock;
            if (emit == null || !emit.SkipSemicolonOnStatement)
            {
                WriteSemiColon(true);
            }
            else
            {
                WriteNewLine();
            }
        }
    }
}
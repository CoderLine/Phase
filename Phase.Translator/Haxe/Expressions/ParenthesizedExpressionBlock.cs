using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class ParenthesizedExpressionBlock : AbstractHaxeScriptEmitterBlock<ParenthesizedExpressionSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteOpenParentheses();
            await EmitTreeAsync(Node.Expression, cancellationToken);
            WriteCloseParentheses();
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    class ConditionalExpressionBlock : AbstractHaxeScriptEmitterBlock<ConditionalExpressionSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            await EmitTreeAsync(Node.Condition, cancellationToken);
            Write(" ? ");
            await EmitTreeAsync(Node.WhenTrue, cancellationToken);
            Write(" : ");
            await EmitTreeAsync(Node.WhenFalse, cancellationToken);
        }
    }
}
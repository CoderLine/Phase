using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class ThrowBlock : AbstractHaxeScriptEmitterBlock<ThrowStatementSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteThrow();
            await EmitTreeAsync(Node.Expression, cancellationToken);
            WriteSemiColon(true);
        }
    }
}
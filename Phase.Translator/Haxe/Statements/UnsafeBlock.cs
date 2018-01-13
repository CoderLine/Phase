using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class UnsafeBlock : AbstractHaxeScriptEmitterBlock<UnsafeStatementSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            await EmitTreeAsync(Node.Block, cancellationToken);
        }
    }
}
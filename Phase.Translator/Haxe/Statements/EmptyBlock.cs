using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class EmptyBlock : AbstractHaxeScriptEmitterBlock<EmptyStatementSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
        }
    }
}
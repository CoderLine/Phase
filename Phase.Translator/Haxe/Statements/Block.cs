using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class Block : AbstractHaxeScriptEmitterBlock<BlockSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            BeginBlock();

            foreach (var statementSyntax in Node.Statements)
            {
                await EmitTreeAsync(statementSyntax, cancellationToken);
            }

            EndBlock();
        }
    }
}

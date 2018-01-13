using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class FixedBlock : AbstractHaxeScriptEmitterBlock<FixedStatementSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            await EmitTreeAsync(Node.Declaration, cancellationToken);
            WriteSemiColon(true);
            await EmitTreeAsync(Node.Statement, cancellationToken);
        }
    }
}
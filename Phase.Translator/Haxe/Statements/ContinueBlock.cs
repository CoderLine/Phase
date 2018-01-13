using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class ContinueBlock : AbstractHaxeScriptEmitterBlock<ContinueStatementSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            Write("continue");
            WriteSemiColon(true);
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class YieldBlock : AbstractHaxeScriptEmitterBlock<YieldStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new PhaseCompilerException("Yield statements are not yet supported");
            // TODO: use something like https://github.com/Atry/haxe-continuation
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class UnsafeBlock : AbstractHaxeScriptEmitterBlock<UnsafeStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            EmitTree(Node.Block, cancellationToken);
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class CheckedBlock : AbstractHaxeScriptEmitterBlock<CheckedStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            EmitTree(Node.Block, cancellationToken);
        }
    }
}
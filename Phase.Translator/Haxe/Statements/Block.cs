using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class Block : AbstractHaxeScriptEmitterBlock<BlockSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            BeginBlock();

            foreach (var statementSyntax in Node.Statements)
            {
                EmitTree(statementSyntax, cancellationToken);
            }

            EndBlock();
        }
    }
}

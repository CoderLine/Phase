using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class FixedBlock : AbstractHaxeScriptEmitterBlock<FixedStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            EmitTree(Node.Declaration, cancellationToken);
            WriteSemiColon(true);
            EmitTree(Node.Statement, cancellationToken);
        }
    }
}
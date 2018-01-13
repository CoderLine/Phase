using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class ReturnBlock : AbstractHaxeScriptEmitterBlock<ReturnStatementSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Node.Expression != null)
            {
                WriteReturn(true);
                await EmitTreeAsync(Node.Expression, cancellationToken);
            }
            else
            {
                WriteReturn(false);
            }
            WriteSemiColon(true);
            WriteNewLine();
        }
    }
}
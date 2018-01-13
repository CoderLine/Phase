using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class IfBlock : AbstractHaxeScriptEmitterBlock<IfStatementSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteIf();
            WriteOpenParentheses();
            await EmitTreeAsync(Node.Condition, cancellationToken);
            WriteCloseParentheses();
            WriteNewLine();

            await EmitTreeAsync(Node.Statement, cancellationToken);

            if (Node.Else != null)
            {
                WriteElse();
                await EmitTreeAsync(Node.Else.Statement, cancellationToken);
            }
        }
    }
}
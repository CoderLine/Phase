using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class WhileBlock : AbstractHaxeScriptEmitterBlock<WhileStatementSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteWhile();
            WriteOpenParentheses();
            await EmitTreeAsync(Node.Condition, cancellationToken);
            WriteCloseParentheses();
            WriteNewLine();
            await EmitTreeAsync(Node.Statement, cancellationToken);
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class DoWhileBlock : AbstractHaxeScriptEmitterBlock<DoStatementSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteDo();
            await EmitTreeAsync(Node.Statement, cancellationToken);
            if (Node.Statement.Kind() == SyntaxKind.Block)
            {
                WriteSpace();
            }

            WriteWhile();
            WriteOpenParentheses();
            await EmitTreeAsync(Node.Condition, cancellationToken);
            WriteCloseParentheses();
            WriteSemiColon(true);
        }
    }
}
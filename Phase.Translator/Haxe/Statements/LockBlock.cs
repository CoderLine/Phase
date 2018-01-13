using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class LockBlock : AbstractHaxeScriptEmitterBlock<LockStatementSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            Write(PhaseConstants.Phase);
            WriteDot();
            Write("Lock");
            WriteOpenParentheses();

            await EmitTreeAsync(Node.Expression, cancellationToken);
            WriteComma();

            WriteFunction();
            WriteOpenCloseParentheses();

            if (Node.Kind() == SyntaxKind.Block)
            {
                await EmitTreeAsync(Node.Statement, cancellationToken);
            }
            else
            {
                BeginBlock();
                await EmitTreeAsync(Node.Statement, cancellationToken);
                EndBlock();
            }

            WriteCloseParentheses();
            WriteSemiColon(true);
        }
    }
}
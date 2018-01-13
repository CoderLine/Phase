using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class LockBlock : AbstractHaxeScriptEmitterBlock<LockStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            Write(PhaseConstants.Phase);
            WriteDot();
            Write("Lock");
            WriteOpenParentheses();

            EmitTree(Node.Expression, cancellationToken);
            WriteComma();

            WriteFunction();
            WriteOpenCloseParentheses();

            if (Node.Kind() == SyntaxKind.Block)
            {
                EmitTree(Node.Statement, cancellationToken);
            }
            else
            {
                BeginBlock();
                EmitTree(Node.Statement, cancellationToken);
                EndBlock();
            }

            WriteCloseParentheses();
            WriteSemiColon(true);
        }
    }
}
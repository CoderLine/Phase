using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class ForBlock : AbstractHaxeScriptEmitterBlock<ForStatementSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            BeginBlock();

            if (Node.Declaration != null)
            {
                await EmitTreeAsync(Node.Declaration, cancellationToken);
            }
            else if (Node.Initializers.Count > 0)
            {
                foreach (var initializer in Node.Initializers)
                {
                    await EmitTreeAsync(initializer, cancellationToken);
                    WriteSemiColon(true);
                }
            }


            WriteWhile();
            WriteOpenParentheses();
            await EmitTreeAsync(Node.Condition, cancellationToken);
            WriteCloseParentheses();

            WriteNewLine();
            BeginBlock();

            if (Node.Statement.Kind() == SyntaxKind.Block)
            {
                foreach (var statement in ((BlockSyntax)Node.Statement).Statements)
                {
                    await EmitTreeAsync(statement, cancellationToken);
                }
            }
            else
            {
                await EmitTreeAsync(Node.Statement, cancellationToken);
            }

            foreach (var incrementor in Node.Incrementors)
            {
                await EmitTreeAsync(incrementor, cancellationToken);
                WriteSemiColon(true);
            }

            EndBlock();
            EndBlock();
        }
    }
}
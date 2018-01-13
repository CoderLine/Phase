using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class ForEachBlock : AbstractHaxeScriptEmitterBlock<ForEachStatementSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteFor();
            WriteOpenParentheses();
            Write(Node.Identifier.ValueText);
            Write(" in (");
            await EmitTreeAsync(Node.Expression, cancellationToken);
            Write(").GetEnumerator()");
            WriteCloseParentheses();
            WriteNewLine();
            await EmitTreeAsync(Node.Statement, cancellationToken);
        }
    }
}
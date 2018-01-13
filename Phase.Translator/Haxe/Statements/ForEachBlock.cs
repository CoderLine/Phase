using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class ForEachBlock : AbstractHaxeScriptEmitterBlock<ForEachStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteFor();
            WriteOpenParentheses();
            Write(Node.Identifier.ValueText);
            Write(" in (");
            EmitTree(Node.Expression, cancellationToken);
            Write(").GetEnumerator()");
            WriteCloseParentheses();
            WriteNewLine();
            EmitTree(Node.Statement, cancellationToken);
        }
    }
}
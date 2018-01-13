using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class UsingBlock : AbstractHaxeScriptEmitterBlock<UsingStatementSyntax>
    {
        private static int _recursiveUsing = 0;
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            BeginBlock();

            string resourceName;
            if (Node.Declaration == null)
            {
                resourceName = "__usingResource" + _recursiveUsing;
                Write("var ", resourceName, " = ");
                EmitTree(Node.Expression, cancellationToken);
                _recursiveUsing++;
                WriteSemiColon(true);
            }
            else
            {
                resourceName = Node.Declaration.Variables.First().Identifier.Text;
                EmitTree(Node.Declaration, cancellationToken);
            }

            Write(PhaseConstants.Phase);
            WriteDot();
            Write("Using");
            WriteOpenParentheses();
            Write(resourceName);
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
            WriteSemiColon();

            if (Node.Declaration == null)
            {
                _recursiveUsing--;
            }

            EndBlock();
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class ForBlock : CommentedNodeEmitBlock<ForStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            BeginBlock();

            if (Node.Declaration != null)
            {
                EmitTree(Node.Declaration, cancellationToken);
            }
            else if (Node.Initializers.Count > 0)
            {
                foreach (var initializer in Node.Initializers)
                {
                    EmitTree(initializer, cancellationToken);
                    WriteSemiColon(true);
                }
            }


            WriteWhile();
            WriteOpenParentheses();
            EmitTree(Node.Condition, cancellationToken);
            WriteCloseParentheses();

            WriteNewLine();
            BeginBlock();

            if (Node.Statement.Kind() == SyntaxKind.Block)
            {
                foreach (var statement in ((BlockSyntax)Node.Statement).Statements)
                {
                    EmitTree(statement, cancellationToken);
                }
            }
            else
            {
                EmitTree(Node.Statement, cancellationToken);
            }

            foreach (var incrementor in Node.Incrementors)
            {
                EmitTree(incrementor, cancellationToken);
                WriteSemiColon(true);
            }

            EndBlock();
            EndBlock();
        }
    }
}
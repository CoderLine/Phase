using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Statements
{
    public class ForBlock : CommentedNodeEmitBlock<ForStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteFor();

            WriteOpenParentheses();
            if (Node.Declaration != null)
            {
                EmitTree(Node.Declaration, cancellationToken);
            }
            else if (Node.Initializers.Count > 0)
            {
                for (var i = 0; i < Node.Initializers.Count; i++)
                {
                    if (i > 0) WriteComma();
                    var initializer = Node.Initializers[i];
                    EmitTree(initializer, cancellationToken);
                }
                WriteSemiColon();
            }


            EmitTree(Node.Condition, cancellationToken);
            WriteSemiColon();

            for (var i = 0; i < Node.Incrementors.Count; i++)
            {
                if (i > 0) Write(", ");
                var incrementor = Node.Incrementors[i];
                EmitTree(incrementor, cancellationToken);
            }

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
        }
    }
}
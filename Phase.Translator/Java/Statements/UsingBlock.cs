using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Statements
{
    public class UsingBlock : CommentedNodeEmitBlock<UsingStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            BeginBlock();

            string resourceName;
            if (Node.Declaration == null)
            {
                resourceName = "__usingResource" + EmitterContext.RecursiveUsing;
                Write("var ", resourceName, " = ");
                EmitTree(Node.Expression, cancellationToken);
                WriteSemiColon(true);
            }
            else
            {
                resourceName = Node.Declaration.Variables.First().Identifier.Text;
                EmitTree(Node.Declaration, cancellationToken);
            }

            EmitterContext.RecursiveUsing++;

            Write("try");
            BeginBlock();
            EmitTree(Node.Statement, cancellationToken);
            EndBlock();

            EmitterContext.RecursiveUsing--;

            Write("finally");
            BeginBlock();

            Write("if(", resourceName, " != null) { ", resourceName, ".dispose(); }");
            WriteNewLine();

            EndBlock();

            EndBlock();
        }
    }
}
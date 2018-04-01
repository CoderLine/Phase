using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Statements
{
    public class UsingBlock : CommentedNodeEmitBlock<UsingStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            BeginBlock();

            string resourceName;
            string usingName = "_u";
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

            if (EmitterContext.RecursiveUsing > 0)
            {
                usingName += EmitterContext.RecursiveUsing;
            }

            EmitterContext.RecursiveUsing++;

            Write("Phase::Using ");
            Write(usingName);
            WriteOpenParentheses();
            Write(resourceName);
            WriteCloseParentheses();
            WriteSemiColon(true);

            EmitTree(Node.Statement, cancellationToken);
            EmitterContext.RecursiveUsing--;

            EndBlock();
        }
    }
}
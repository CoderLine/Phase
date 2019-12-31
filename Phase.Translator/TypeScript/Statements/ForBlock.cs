using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript
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
            }

            WriteSemiColon(false);

            EmitTree(Node.Condition, cancellationToken);

            WriteSemiColon();

            EmitterContext.CurrentForIncrementors.Push(Node.Incrementors);

            for (var i = 0; i < Node.Incrementors.Count; i++)
            {
                if (i > 0) WriteComma();
                var incrementor = Node.Incrementors[i];
                EmitTree(incrementor, cancellationToken);
            }

            WriteCloseParentheses();

            EmitTree(Node.Statement, cancellationToken);

            EmitterContext.CurrentForIncrementors.Pop();
        }
    }
}
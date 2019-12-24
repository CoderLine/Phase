using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript.Expressions
{
    public class VariableDeclarationBlock : AbstractTypeScriptEmitterBlock<VariableDeclarationSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            for (int i = 0; i < Node.Variables.Count; i++)
            {
                WriteVar();
                var variable = Node.Variables[i];
                Write(variable.Identifier.Text);

                WriteColon();
                WriteType(Node.Type);

                if (variable.Initializer != null)
                {
                    Write(" = ");
                    EmitTree(variable.Initializer.Value, cancellationToken);
                }

                WriteSemiColon();
            }
        }

        protected override void EndEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            base.EndEmit(cancellationToken);
            WriteNewLine();
        }
    }
}
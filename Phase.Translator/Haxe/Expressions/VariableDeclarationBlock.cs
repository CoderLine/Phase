using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class VariableDeclarationBlock : AbstractHaxeScriptEmitterBlock<VariableDeclarationSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
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
                    await EmitTreeAsync(variable.Initializer.Value, cancellationToken);
                }

                WriteSemiColon(true);
            }

        }
    }
}
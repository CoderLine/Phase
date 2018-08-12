using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    public class VariableDeclarationBlock : AbstractKotlinEmitterBlock<VariableDeclarationSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            for (int i = 0; i < Node.Variables.Count; i++)
            {
                Write("var ");
                var variable = Node.Variables[i];
                Write(" ", variable.Identifier.Text.Replace("@", ""));

                Write(" : ");
                var type = Emitter.GetTypeSymbol(Node.Type);
                Write(Emitter.GetTypeName(type, false, false));

                if (variable.Initializer != null)
                {
                    Write(" = ");
                    EmitTree(variable.Initializer.Value, cancellationToken);
                }
                WriteNewLine();
            }
        }
    }
}
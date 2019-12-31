using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript.Expressions
{
    public class VariableDeclarationBlock : AbstractTypeScriptEmitterBlock<VariableDeclarationSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            Write("let ");
            for (int i = 0; i < Node.Variables.Count; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                }
                var variable = Node.Variables[i];
                Write(variable.Identifier.Text);

                WriteColon();
                var type = Emitter.GetTypeSymbol(Node.Type);
                WriteType(type);
                EmitterContext.ImportType(type);

                if (variable.Initializer != null)
                {
                    Write(" = ");
                    EmitTree(variable.Initializer.Value, cancellationToken);
                }

                if (Node.Parent.Kind() != SyntaxKind.ForStatement)
                {
                    WriteSemiColon();
                }
            }
        }

        protected override void EndEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            base.EndEmit(cancellationToken);
            if (Node.Parent.Kind() != SyntaxKind.ForStatement)
            {
                WriteNewLine();
            }
        }
    }
}
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    public class VariableDeclarationBlock : AbstractCppEmitterBlock<VariableDeclarationSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            for (int i = 0; i < Node.Variables.Count; i++)
            {
                if (i == 0)
                {
                    var type = Emitter.GetTypeSymbol(Node.Type);
                    Write(Emitter.GetTypeName(type, false, false, CppEmitter.TypeNamePointerKind.SharedPointerDeclaration));
                    EmitterContext.ImportType(type);
                }
                else
                {
                    WriteComma();
                }
                var variable = Node.Variables[i];
                Write(" ", variable.Identifier.Text.Replace("@", ""));

                if (variable.Initializer != null)
                {
                    Write(" = ");
                    EmitTree(variable.Initializer.Value, cancellationToken);
                }

            }
            WriteSemiColon();
        }

        protected override void EndEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            base.EndEmit(cancellationToken);
            WriteNewLine();
        }
    }
}
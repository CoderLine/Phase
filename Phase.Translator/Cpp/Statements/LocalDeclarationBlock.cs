using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Statements
{
    public class LocalDeclarationBlock : CommentedNodeEmitBlock<LocalDeclarationStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var var in Node.Declaration.Variables)
            {
                var type = Emitter.GetTypeSymbol(Node.Declaration.Type);
                EmitterContext.ImportType(type);

                Write(Emitter.GetTypeName(type, false, false, CppEmitter.TypeNamePointerKind.SharedPointerDeclaration));
                Write(" ", var.Identifier.Text.Replace("@", ""));

                if (var.Initializer != null)
                {
                    Write(" = ");
                    EmitTree(var.Initializer.Value, cancellationToken);
                }

                WriteSemiColon(true);
            }
        }
    }
}
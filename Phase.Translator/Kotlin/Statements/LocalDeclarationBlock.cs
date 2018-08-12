using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Statements
{
    public class LocalDeclarationBlock : CommentedNodeEmitBlock<LocalDeclarationStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var var in Node.Declaration.Variables)
            {
                var type = Emitter.GetTypeSymbol(Node.Declaration.Type);

                Write("var ", var.Identifier.Text.Replace("@", ""), " : ");
                Write(Emitter.GetTypeName(type, false, false));
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
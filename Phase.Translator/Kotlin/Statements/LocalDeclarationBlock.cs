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
                var local = Emitter.GetDeclaredSymbol(var);

                Write("var ", EmitterContext.GetSymbolName(local), " : ");
                Write(Emitter.GetTypeName(type, false, false));
                Write(" = ");
                if (var.Initializer != null)
                {
                    EmitTree(var.Initializer.Value, cancellationToken);
                }
                else
                {
                    Write(Emitter.GetDefaultValue(type));
                }
                WriteSemiColon(true);
            }
        }
    }
}
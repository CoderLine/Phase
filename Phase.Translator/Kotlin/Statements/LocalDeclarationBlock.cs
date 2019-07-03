using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
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


                Write("var ", EmitterContext.GetSymbolName(local));

                var nullable = Node.Declaration.Type.Kind() == SyntaxKind.NullableType;
                if (!Node.Declaration.Type.IsVar)
                {
                    Write(" : ", Emitter.GetTypeName(type, false, false, nullable));
                }
                
                if (var.Initializer != null)
                {
                    Write(" = ");
                    EmitTree(var.Initializer.Value, cancellationToken);
                }
                else
                {
                    var defaultValue = Emitter.GetDefaultValue(type);
                    if (defaultValue != "null" && !nullable)
                    {
                        Write(" = ", defaultValue);
                    }
                }
                WriteSemiColon(true);
            }
        }
    }
}
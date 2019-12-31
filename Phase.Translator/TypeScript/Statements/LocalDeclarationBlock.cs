using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript
{
    public class LocalDeclarationBlock : CommentedNodeEmitBlock<LocalDeclarationStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            Write("let ");
            for (var i = 0; i < Node.Declaration.Variables.Count; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                }
                var var = Node.Declaration.Variables[i];
                var isRef = Emitter.IsRefVariable(var);
                var type = Emitter.GetTypeSymbol(Node.Declaration.Type);
                EmitterContext.ImportType(type);

                Write(var.Identifier.ValueText);
                WriteColon();
                if (isRef)
                {
                    Write("CsRef<");
                    WriteType(type);
                    Write(">");
                }
                else
                {
                    WriteType(type);
                }

                if (var.Initializer != null)
                {
                    Write(" = ");
                    if (isRef)
                    {
                        Write("new CsRef(");
                    }

                    EmitTree(var.Initializer, cancellationToken);
                    if (isRef)
                    {
                        Write(")");
                    }
                }
                else if (isRef)
                {
                    Write(" = new CsRef(");
                    Write(Emitter.GetDefaultValue(type));
                    Write(")");
                }
            }
            

            if (Node.Parent.Kind() != SyntaxKind.ForStatement)
            {
                WriteSemiColon(true);
            }
        }
    }
}
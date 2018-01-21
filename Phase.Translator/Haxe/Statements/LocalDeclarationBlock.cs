using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class LocalDeclarationBlock : CommentedNodeEmitBlock<LocalDeclarationStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var var in Node.Declaration.Variables)
            {
                var isRef = Emitter.IsRefVariable(var);
                var type = Emitter.GetTypeSymbol(Node.Declaration.Type);

                WriteVar();
                Write(var.Identifier.ValueText);

                WriteSpace();
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
                else if(isRef)
                {
                    Write(" = new CsRef(");
                    Write(Emitter.GetDefaultValue(type));
                    Write(")");
                }

                WriteSemiColon(true);
            }
        }
    }
}
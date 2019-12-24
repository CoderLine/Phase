using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Phase.Translator.TypeScript
{
    public class ReturnBlock : CommentedNodeEmitBlock<ReturnStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Node.Expression != null)
            {
                WriteReturn(true);
                EmitTree(Node.Expression, cancellationToken);
            }
            else if (EmitterContext.SetterMethod != null)
            {
                WriteReturn(true);
                var property = (IPropertySymbol)EmitterContext.SetterMethod.AssociatedSymbol;
                if (property.GetMethod != null)
                {
                    Write(EmitterContext.GetMethodName(property.GetMethod));
                    WriteOpenParentheses();
                    if (property.IsIndexer)
                    {
                        for (int i = 0; i < property.GetMethod.Parameters.Length; i++)
                        {
                            if (i > 0)
                            {
                                WriteComma();
                            }
                            Write(property.GetMethod.Parameters[i].Name);
                        }
                    }
                    WriteCloseParentheses();
                }
                else
                {
                    Write(EmitterContext.SetterMethod.Parameters.Last().Name);
                }
            }
            else
            {
                WriteReturn(false);
            }
            WriteSemiColon(true);
            WriteNewLine();
        }
    }
}
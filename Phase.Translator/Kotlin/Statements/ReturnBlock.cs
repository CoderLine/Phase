using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Statements
{
    public class ReturnBlock : CommentedNodeEmitBlock<ReturnStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Node.Expression != null)
            {
                WriteReturn(false);
                var lambdaName = EmitterContext.GetLambdaNameForReturn();
                if (lambdaName != null)
                {
                    Write("@", lambdaName);
                }
                Write(" ");
                EmitTree(Node.Expression, cancellationToken);
            }
            else if (EmitterContext.SetterMethod != null)
            {
                WriteReturn(false);
                var lambdaName = EmitterContext.GetLambdaNameForReturn();
                if (lambdaName != null)
                {
                    Write("@", lambdaName);
                }
                Write(" ");

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
                var lambdaName = EmitterContext.GetLambdaNameForReturn();
                if (lambdaName != null)
                {
                    Write("@", lambdaName);
                }
            }
            WriteSemiColon(true);
            WriteNewLine();
        }
    }
}
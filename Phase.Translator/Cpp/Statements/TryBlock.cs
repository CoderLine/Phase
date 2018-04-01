using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Statements
{
    public class TryBlock : CommentedNodeEmitBlock<TryStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Node.Finally != null)
            {
                BeginBlock();
                var finallyName = "_f";
                if (EmitterContext.RecursiveFinally > 0)
                {
                    finallyName += EmitterContext.RecursiveFinally;
                }
                Write("Phase::Finally ", finallyName);
                WriteOpenParentheses();

                Write("[&, this]");
                WriteNewLine();

                EmitTree(Node.Finally.Block, cancellationToken);
                WriteCloseParentheses();

                WriteCloseParentheses();
                WriteSemiColon(true);
                EmitterContext.RecursiveFinally++;
            }


            if (Node.Catches.Count == 0)
            {
                EmitTree(Node.Block, cancellationToken);
            }
            else
            {
                Write("try");
                WriteNewLine();
                EmitTree(Node.Block, cancellationToken);

                foreach (var catchClauseSyntax in Node.Catches)
                {
                    try
                    {
                        var nameAdded = false;
                        EmitterContext.RecursiveCatch++;
                        Write("catch");
                        if (catchClauseSyntax.Declaration != null)
                        {
                            WriteOpenParentheses();
                            var exceptionType = Emitter.GetTypeSymbol(catchClauseSyntax.Declaration.Type);
                            EmitterContext.ImportType(exceptionType);
                            WriteType(exceptionType);

                            Write(" &");

                            if (!string.IsNullOrEmpty(catchClauseSyntax.Declaration.Identifier.Text))
                            {
                                EmitterContext.CurrentExceptionName.Push(catchClauseSyntax.Declaration.Identifier.ValueText);
                                nameAdded = true;
                                Write(catchClauseSyntax.Declaration.Identifier.ValueText);
                            }
                            WriteCloseParentheses();
                        }
                        else
                        {
                            WriteOpenParentheses();
                            Write("...");
                            WriteCloseParentheses();
                        }
                        WriteNewLine();
                        EmitTree(catchClauseSyntax.Block, cancellationToken);
                        if (nameAdded)
                        {
                            EmitterContext.CurrentExceptionName.Pop();
                        }
                    }
                    finally
                    {
                        EmitterContext.RecursiveCatch--;
                    }
                }
            }

            if (Node.Finally != null)
            {
                EmitterContext.RecursiveFinally--;
            }
        }
    }
}
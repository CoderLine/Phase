using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Statements
{
    public class TryBlock : CommentedNodeEmitBlock<TryStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
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

                        if (!string.IsNullOrEmpty(catchClauseSyntax.Declaration.Identifier.Text))
                        {
                            EmitterContext.CurrentExceptionName.Push(catchClauseSyntax.Declaration.Identifier.ValueText);
                            nameAdded = true;
                            Write(catchClauseSyntax.Declaration.Identifier.ValueText);
                        }
                        else
                        {
                            var name = "__ex";
                            if (EmitterContext.RecursiveCatch > 0) name += EmitterContext.RecursiveCatch;
                            EmitterContext.CurrentExceptionName.Push(catchClauseSyntax.Declaration.Identifier.ValueText);
                            nameAdded = true;
                            Write(name);
                        }

                        Write(" : ");
                        Write(Emitter.GetTypeName(exceptionType, false, false));

                        WriteCloseParentheses();
                    }
                    else
                    {
                        WriteOpenParentheses();
                        Write("__e : Throwable");
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

            if (Node.Finally != null)
            {
                Write("finally");
                EmitTree(Node.Finally.Block, cancellationToken);
            }
        }
    }
}
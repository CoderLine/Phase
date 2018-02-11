using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class TryBlock : CommentedNodeEmitBlock<TryStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Node.Finally != null)
            {
                Write(PhaseConstants.Phase);
                WriteDot();
                Write("Finally");
                WriteOpenParentheses();

                WriteFunction();
                WriteOpenCloseParentheses();
                BeginBlock();
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
                    var variable = "__e" + ((EmitterContext.RecursiveCatch > 0) ? EmitterContext.RecursiveCatch.ToString() : "");
                    try
                    {
                        EmitterContext.RecursiveCatch++;
                        Write("catch");
                        if (catchClauseSyntax.Declaration != null)
                        {
                            WriteOpenParentheses();
                            if (string.IsNullOrEmpty(catchClauseSyntax.Declaration.Identifier.Text))
                            {
                                EmitterContext.CurrentExceptionName.Push(variable);
                                Write(variable);
                            }
                            else
                            {
                                EmitterContext.CurrentExceptionName.Push(catchClauseSyntax.Declaration.Identifier.ValueText);
                                Write(catchClauseSyntax.Declaration.Identifier.ValueText);
                            }
                            WriteColon();
                            WriteType(catchClauseSyntax.Declaration.Type);
                            WriteCloseParentheses();
                        }
                        else
                        {
                            WriteOpenParentheses();
                            Write(variable);
                            EmitterContext.CurrentExceptionName.Push(variable);
                            WriteColon();
                            Write("Dynamic");
                            WriteCloseParentheses();
                        }
                        WriteNewLine();
                        EmitTree(catchClauseSyntax.Block, cancellationToken);
                        EmitterContext.CurrentExceptionName.Pop();
                    }
                    finally
                    {
                        EmitterContext.RecursiveCatch--;
                    }
                }
            }

            if (Node.Finally != null)
            {
                EndBlock();
                WriteComma();
                WriteFunction();
                WriteOpenCloseParentheses();
                EmitTree(Node.Finally.Block, cancellationToken);
                WriteCloseParentheses();
                WriteSemiColon(true);
            }
        }
    }
}
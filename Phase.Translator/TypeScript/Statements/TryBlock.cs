using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript
{
    public class TryBlock : CommentedNodeEmitBlock<TryStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Node.Catches.Count == 0)
            {
                EmitTree(Node.Block, cancellationToken);
            }
            else
            {
                Write("try");
                WriteNewLine();
                EmitTree(Node.Block, cancellationToken);

                if (Node.Catches.Count > 0)
                {
                    var variable = "__e" + ((EmitterContext.RecursiveCatch > 0) ? EmitterContext.RecursiveCatch.ToString() : "");
                    EmitterContext.RecursiveCatch++;
                    Write("catch(", variable, ") ");
                    BeginBlock();

                    for (var i = 0; i < Node.Catches.Count; i++)
                    {
                        var catchClauseSyntax = Node.Catches[i];
                        if (catchClauseSyntax.Declaration != null)
                        {
                            if (i > 0)
                            {
                                WriteElse();
                            }
                            WriteIf();
                            WriteOpenParentheses();
                            Write(variable, " instanceof ");
                            var type = Emitter.GetTypeSymbol(catchClauseSyntax.Declaration.Type);
                            WriteType(type);
                            EmitterContext.ImportType(type);
                            WriteCloseParentheses();
                            BeginBlock();

                            if (string.IsNullOrEmpty(catchClauseSyntax.Declaration.Identifier.Text))
                            {
                                EmitterContext.CurrentExceptionName.Push(variable);
                            }
                            else
                            {
                                EmitterContext.CurrentExceptionName.Push(catchClauseSyntax.Declaration.Identifier.ValueText);
                                Write("const ", catchClauseSyntax.Declaration.Identifier.ValueText, " = ", variable,
                                    " as ");
                                WriteType(type);
                                WriteSemiColon(true);
                            }

                            foreach (var statement in catchClauseSyntax.Block.Statements)
                            {
                                EmitTree(statement, cancellationToken);
                            }
                            
                            EndBlock();
                        }
                        else
                        {
                            EmitterContext.CurrentExceptionName.Push(variable);
                            if (i > 0)
                            {
                                WriteElse();
                                BeginBlock();
                            }

                            foreach (var statement in catchClauseSyntax.Block.Statements)
                            {
                                EmitTree(statement, cancellationToken);
                            }
                            
                            if (i > 0)
                            {
                                EndBlock();
                            }
                        }

                        EmitterContext.CurrentExceptionName.Pop();
                    }

                    Write("throw ", variable);
                    WriteSemiColon(true);

                    EmitterContext.RecursiveCatch--;
                    EndBlock();
                }
            }

            if (Node.Finally != null)
            {
                WriteFinally();
                EmitTree(Node.Finally.Block, cancellationToken);
            }
        }
    }
}
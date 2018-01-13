using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class TryBlock : AbstractHaxeScriptEmitterBlock<TryStatementSyntax>
    {
        private static int _recursiveCatch;
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
                EmitTree(Node.Block, cancellationToken);

                foreach (var catchClauseSyntax in Node.Catches)
                {
                    var variable = "__e" + ((_recursiveCatch > 0) ? _recursiveCatch.ToString() : "");
                    try
                    {
                        _recursiveCatch++;
                        Write("catch");
                        if (catchClauseSyntax.Declaration != null)
                        {
                            WriteOpenParentheses();
                            if (string.IsNullOrEmpty(catchClauseSyntax.Declaration.Identifier.Text))
                            {
                                Write(variable);
                            }
                            else
                            {
                                Write(catchClauseSyntax.Declaration.Identifier.Value);
                            }
                            WriteColon();
                            WriteType(catchClauseSyntax.Declaration.Type);
                            WriteCloseParentheses();
                        }
                        else
                        {
                            WriteOpenParentheses();
                            Write(variable);
                            WriteColon();
                            WriteType(
                                EmitterContext.CurrentType.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Exception)
                                    .FullName));
                            WriteCloseParentheses();
                        }
                        EmitTree(catchClauseSyntax.Block, cancellationToken);
                    }
                    finally
                    {
                        _recursiveCatch--;
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Statements
{
    public class SwitchBlock : CommentedNodeEmitBlock<SwitchStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            Write("when");
            WriteOpenParentheses();

            var type = Emitter.GetTypeInfo(Node.Expression, cancellationToken);
            EmitTree(Node.Expression, cancellationToken);
            WriteCloseParentheses();
            WriteNewLine();
            BeginBlock();

            foreach (var section in Node.Sections)
            {
                var sectionHasDefault = section.Labels.Any(l => l.Kind() == SyntaxKind.DefaultSwitchLabel);
                if (sectionHasDefault)
                {
                    Write("else -> ");
                    WriteNewLine();
                }
                else
                {
                    for (var i = 0; i < section.Labels.Count; i++)
                    {
                        if (i > 0) WriteComma();
                        EmitterContext.IsCaseLabel = true;
                        var label = section.Labels[i];
                        switch (label.Kind())
                        {
                            case SyntaxKind.CaseSwitchLabel:
                                var caseLabel = (CaseSwitchLabelSyntax)label;
                                EmitTree(caseLabel.Value, cancellationToken);
                                break;
                            default:
                                Debugger.Break();
                                break;
                        }
                        EmitterContext.IsCaseLabel = false;
                    }
                    Write(" -> ");
                }
                BeginBlock();
                foreach (var statement in section.Statements)
                {
                    EmitTree(statement, cancellationToken);
                }
                EndBlock();
            }

            EndBlock();
        }
    }
}
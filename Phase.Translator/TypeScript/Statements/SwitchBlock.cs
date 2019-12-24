using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript
{
    public class SwitchBlock : CommentedNodeEmitBlock<SwitchStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteSwitch();
            WriteOpenParentheses();
            EmitTree(Node.Expression, cancellationToken);
            WriteCloseParentheses();
            WriteNewLine();
            BeginBlock();

            bool hasDefault = false;

            foreach (var section in Node.Sections)
            {
                var sectionHasDefault = section.Labels.Any(l => l.Kind() == SyntaxKind.DefaultSwitchLabel);
                if (sectionHasDefault)
                {
                    Write("default");
                    hasDefault = true;
                }
                else
                {
                    for (var i = 0; i < section.Labels.Count; i++)
                    {
                        if (i == 0) Write("case ");
                        else Write(", ");

                        EmitterContext.IsCaseLabel = true;
                        var label = section.Labels[i];
                        switch (label.Kind())
                        {
                            case SyntaxKind.CaseSwitchLabel:
                                var caseLabel = (CaseSwitchLabelSyntax) label;
                                EmitTree(caseLabel.Value, cancellationToken);
                                break;
                            default:
                                Debugger.Break();
                                break;
                        }
                        EmitterContext.IsCaseLabel = false;
                    }
                }
                WriteColon();
                WriteNewLine();

                Indent();
                foreach (var statement in section.Statements)
                {
                    EmitTree(statement, cancellationToken);
                }
                Outdent();
            }

            if (!hasDefault)
            {
                Write("default:");
                WriteNewLine();
            }

            EndBlock();
        }
    }
}
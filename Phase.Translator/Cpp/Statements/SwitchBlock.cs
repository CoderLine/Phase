using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Statements
{
    public class SwitchBlock : CommentedNodeEmitBlock<SwitchStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteSwitch();
            WriteOpenParentheses();

            var type = Emitter.GetTypeInfo(Node.Expression, cancellationToken);
            if (type.Type.SpecialType == SpecialType.System_String)
            {
                Write("Phase::StringHash(");
            }
            EmitTree(Node.Expression, cancellationToken);
            if (type.Type.SpecialType == SpecialType.System_String)
            {
                Write(")");
            }
            WriteCloseParentheses();
            WriteNewLine();
            BeginBlock();

            foreach (var section in Node.Sections)
            {
                var sectionHasDefault = section.Labels.Any(l => l.Kind() == SyntaxKind.DefaultSwitchLabel);
                if (sectionHasDefault)
                {
                    Write("default");
                    WriteColon();
                    WriteNewLine();
                }
                else
                {
                    for (var i = 0; i < section.Labels.Count; i++)
                    {
                        Write("case ");
                        EmitterContext.IsCaseLabel = true;
                        var label = section.Labels[i];
                        switch (label.Kind())
                        {
                            case SyntaxKind.CaseSwitchLabel:
                                var caseLabel = (CaseSwitchLabelSyntax)label;
                                if (type.Type.SpecialType == SpecialType.System_String)
                                {
                                    Write("PHASE_STRING_HASH(");
                                }
                                EmitTree(caseLabel.Value, cancellationToken);
                                if (type.Type.SpecialType == SpecialType.System_String)
                                {
                                    Write(")");
                                }
                                break;
                            default:
                                Debugger.Break();
                                break;
                        }
                        EmitterContext.IsCaseLabel = false;
                        WriteColon();
                        WriteNewLine();
                    }
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
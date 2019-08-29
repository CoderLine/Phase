using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class InterpolatedStringExpressionBlock : AbstractHaxeScriptEmitterBlock<InterpolatedStringExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            var num = 0;
            var formatArguments = new List<ExpressionSyntax>();
            Write("system.CsString.format(");
            Write("\"");
            foreach (var content in Node.Contents)
            {
                switch (content.Kind())
                {
                    case SyntaxKind.InterpolatedStringText:
                        var text = (InterpolatedStringTextSyntax)content;
                        Write(text.TextToken.ValueText.Replace("\"\"", "\\\""));
                        break;
                    case SyntaxKind.Interpolation:
                        var interpolation = (InterpolationSyntax)content;
                        Write("{");
                        Write(num);
                        if (interpolation.AlignmentClause != null)
                        {
                            Write(",");
                            var value = Emitter.GetConstantValue(interpolation.AlignmentClause.Value,
                                cancellationToken);
                            Write(value.Value);
                        }

                        if (interpolation.FormatClause != null)
                        {
                            Write(":");
                            Write(interpolation.FormatClause.FormatStringToken.ValueText);
                        }
                        Write("}");
                        formatArguments.Add(interpolation.Expression);
                        num++;
                        break;
                }
            }
            Write("\", [");

            for (var i = 0; i < formatArguments.Count; i++)
            {
                if(i> 0) WriteComma();
                EmitTree(formatArguments[i]);
            }

            Write("])");
        }
    }
}
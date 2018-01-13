using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class LiteralExpressionBlock : AbstractHaxeScriptEmitterBlock<LiteralExpressionSyntax>
    {
        private static readonly Regex NewLine = new Regex("(\r\n?)", RegexOptions.Compiled);
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var value = Node.Token.Value;
            switch (Node.Kind())
            {
                case SyntaxKind.NumericLiteralExpression:
                    Write(value);
                    break;
                case SyntaxKind.StringLiteralExpression:
                    if (Node.Token.Text.StartsWith("@"))
                    {
                        var text = Node.Token.Text.Substring(1)
                            .Replace("\\", "\\\\")
                            .Replace("\"\"", "\"")
                            .Replace("\r", "\\r")
                            .Replace("\n", "\\n");
                        Write(text);
                    }
                    else
                    {
                        Write(Node.Token.Text);
                    }
                    break;
                case SyntaxKind.CharacterLiteralExpression:
                    Write((int)(char)value);
                    break;
                case SyntaxKind.TrueLiteralExpression:
                    Write("true");
                    break;
                case SyntaxKind.FalseLiteralExpression:
                    Write("false");
                    break;
                case SyntaxKind.NullLiteralExpression:
                    Write("null");
                    break;
            }
        }
    }
}
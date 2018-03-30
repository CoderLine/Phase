using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class LiteralExpressionBlock : AbstractHaxeScriptEmitterBlock<LiteralExpressionSyntax>
    {
        private static readonly Regex NewLine = new Regex("(\r\n?)", RegexOptions.Compiled);
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            var value = Node.Token.Value;
            switch (Node.Kind())
            {
                case SyntaxKind.NumericLiteralExpression:
                    Write(Node.Token.Text.TrimEnd('f'));
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
                    var cast = Emitter.GetTypeInfo(Node, cancellationToken).ConvertedType;
                    
                    if (EmitterContext.IsCaseLabel)
                    {
                        cast = Emitter.GetPhaseType("System.Int32");
                    }

                    if (cast != null)
                    {
                        switch (cast.SpecialType)
                        {
                            case SpecialType.System_SByte:
                            case SpecialType.System_Byte:
                            case SpecialType.System_Int16:
                            case SpecialType.System_UInt16:
                            case SpecialType.System_Int32:
                            case SpecialType.System_UInt32:
                            case SpecialType.System_Int64:
                            case SpecialType.System_UInt64:
                            case SpecialType.System_Decimal:
                            case SpecialType.System_Single:
                            case SpecialType.System_Double:
                                Write((int)(char)value);
                                return;
                        }
                    }

                    Write("system.Char.fromCode(");
                    Write((int)(char)value);
                    WriteCloseParentheses();
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
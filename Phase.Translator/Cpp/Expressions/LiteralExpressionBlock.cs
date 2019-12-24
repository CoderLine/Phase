﻿using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    public class LiteralExpressionBlock : AbstractCppEmitterBlock<LiteralExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            var value = Node.Token.Value;
            switch (Node.Kind())
            {
                case SyntaxKind.NumericLiteralExpression:
                    var constant = Emitter.GetConstantValue(Node, cancellationToken);
                    if (constant.HasValue)
                    {
                        if (constant.Value is float)
                        {
                            if (Node.Token.Text.EndsWith("f", StringComparison.InvariantCultureIgnoreCase) &&
                                !Node.Token.Text.Contains("."))
                            {
                                Write(Node.Token.Text.Replace("f", ".0f"));
                                return;
                            }
                        }
                    }

                    Write(Node.Token.Text);
                    break;
                case SyntaxKind.StringLiteralExpression:

                    var isBinaryOp = IsBinaryOp();
                    var isParameter = EmitterContext.IsParameter;

                    if (isBinaryOp || isParameter)
                    {
                        Write("System::String(");
                    }
                    
                    if (Node.Token.Text.StartsWith("@"))
                    {
                        var text = Node.Token.Text.Substring(1)
                            .Replace("\\", "\\\\")
                            .Replace("\"\"", "\"")
                            .Replace("\r", "\\r")
                            .Replace("\n", "\\n");
                        Write("L" + text);
                    }
                    else
                    {
                        Write("L" + Node.Token.Text);
                    }

                    if (isBinaryOp || isParameter)
                    {
                        Write(")");
                    }
                    break;
                case SyntaxKind.CharacterLiteralExpression:
                    Write("static_cast<wchar_t>");
                    WriteOpenParentheses();
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
                    Write("nullptr");
                    break;
            }
        }

        private bool IsBinaryOp()
        {
            SyntaxNode parent = Node.Parent;
            while (parent != null)
            {
                if (parent.Kind() == SyntaxKind.ParenthesizedExpression)
                {
                    parent = Node.Parent;
                }
                else
                {
                    break;
                }
            }

            return parent != null && parent is BinaryExpressionSyntax;
        }
    }
}
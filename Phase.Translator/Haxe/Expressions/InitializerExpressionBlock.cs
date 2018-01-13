﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class InitializerExpressionBlock : AbstractHaxeScriptEmitterBlock<InitializerExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteOpenBracket();

            for (int i = 0; i < Node.Expressions.Count; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                }
                EmitTree(Node.Expressions[i], cancellationToken);
            }

            WriteCloseBracket();
        }
    }
}
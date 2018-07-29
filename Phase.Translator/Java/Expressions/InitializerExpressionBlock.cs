using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Expressions
{
    public class InitializerExpressionBlock : AbstractJavaEmitterBlock<InitializerExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            BeginBlock();

            for (var i = 0; i < Node.Expressions.Count; i++)
            {
                var expression = Node.Expressions[i];
                if (i > 0) WriteComma();
                EmitTree(expression, cancellationToken);
            }

            EndBlock();
        }
    }
}
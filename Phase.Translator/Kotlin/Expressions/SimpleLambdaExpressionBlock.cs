using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    class SimpleLambdaExpressionBlock : AutoCastBlockBase<SimpleLambdaExpressionSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteOpenBrace();
            Write(Node.Parameter.Identifier.Text);
            Write(" -> ");
            EmitTree(Node.Body, cancellationToken);
            WriteCloseBrace();
            return AutoCastMode.SkipCast;
        }
    }
}
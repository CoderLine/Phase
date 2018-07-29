using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Expressions
{
    class SimpleLambdaExpressionBlock : AutoCastBlockBase<SimpleLambdaExpressionSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            Write(Node.Parameter.Identifier.Text);
            Write(" -> ");
            EmitTree(Node.Body, cancellationToken);
            return AutoCastMode.SkipCast;
        }
    }
}
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    class ConditionalExpressionBlock : AbstractKotlinEmitterBlock<ConditionalExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            EmitTree(Node.Condition, cancellationToken);
            Write(" ? ");
            EmitTree(Node.WhenTrue, cancellationToken);
            Write(" : ");
            EmitTree(Node.WhenFalse, cancellationToken);
        }
    }
}
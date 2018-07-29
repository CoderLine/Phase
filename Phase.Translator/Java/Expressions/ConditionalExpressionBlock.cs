using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Expressions
{
    class ConditionalExpressionBlock : AbstractJavaEmitterBlock<ConditionalExpressionSyntax>
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
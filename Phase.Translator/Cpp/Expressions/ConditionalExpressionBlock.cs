using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    class ConditionalExpressionBlock : AbstractCppEmitterBlock<ConditionalExpressionSyntax>
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
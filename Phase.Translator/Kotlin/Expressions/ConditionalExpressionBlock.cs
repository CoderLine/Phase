using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    class ConditionalExpressionBlock : AbstractKotlinEmitterBlock<ConditionalExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            Write("if");
            WriteOpenParentheses();
            EmitTree(Node.Condition, cancellationToken);
            WriteCloseParentheses();
            WriteSpace();
            EmitTree(Node.WhenTrue, cancellationToken);
            WriteSpace();
            WriteElse();
            EmitTree(Node.WhenFalse, cancellationToken);
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript.Expressions
{
    class ConditionalExpressionBlock : AbstractTypeScriptEmitterBlock<ConditionalExpressionSyntax>
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
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Translator.Utils;

namespace Phase.Translator.Haxe.Expressions
{
    class ConditionalAccessExpressionBlock : AutoCastBlockBase<ConditionalAccessExpressionSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            EmitTree(Node.Expression, cancellationToken);
            EmitTree(Node.WhenNotNull, cancellationToken);
            return AutoCastMode.Default;
        }
    }
}
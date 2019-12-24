using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript.Expressions
{
    class ThisExpressionBlock : AbstractTypeScriptEmitterBlock<ThisExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            Write("this");
        }
    }
}
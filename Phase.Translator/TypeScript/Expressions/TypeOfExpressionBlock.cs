using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript.Expressions
{
    public class TypeOfExpressionBlock : AbstractTypeScriptEmitterBlock<TypeOfExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteType(Node.Type);
        }
    }
}
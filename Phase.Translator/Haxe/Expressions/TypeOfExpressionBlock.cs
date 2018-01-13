using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class TypeOfExpressionBlock : AbstractHaxeScriptEmitterBlock<TypeOfExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteType(Node.Type);
        }
    }
}
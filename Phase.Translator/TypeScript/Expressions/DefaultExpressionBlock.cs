using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript.Expressions
{
    public class DefaultExpressionBlock : AbstractTypeScriptEmitterBlock<DefaultExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var type = Emitter.GetTypeSymbol(Node.Type);
            Write(Emitter.GetDefaultValue(type));    
        }
    }
}
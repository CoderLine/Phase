using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class DefaultExpressionBlock : AbstractHaxeScriptEmitterBlock<DefaultExpressionSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var type = Emitter.GetTypeSymbol(Node.Type);
            Write(Emitter.GetDefaultValue(type));
        }
    }
}
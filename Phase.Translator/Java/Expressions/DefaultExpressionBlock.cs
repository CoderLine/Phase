using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Expressions
{
    public class DefaultExpressionBlock : AbstractJavaEmitterBlock<DefaultExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var type = Emitter.GetTypeSymbol(Node.Type);
            Write(Emitter.GetDefaultValue(type));
        }
    }
}
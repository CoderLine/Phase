using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    public class AnonymousObjectCreationExpressionBlock : AbstractKotlinEmitterBlock<AnonymousObjectCreationExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            // TODO: new Object() {}
            throw new PhaseCompilerException("Anonymous objects not supported in C++");
        }
    }
}
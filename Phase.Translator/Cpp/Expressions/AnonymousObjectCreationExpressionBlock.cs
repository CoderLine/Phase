using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    public class AnonymousObjectCreationExpressionBlock : AbstractCppEmitterBlock<AnonymousObjectCreationExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            // TODO: maybe make a local struct?
            throw new PhaseCompilerException("Anonymous objects not supported in C++");
        }
    }
}
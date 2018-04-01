using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    public class TypeOfExpressionBlock : AbstractCppEmitterBlock<TypeOfExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException("Basic reflection on all objects to be added");
        }
    }
}
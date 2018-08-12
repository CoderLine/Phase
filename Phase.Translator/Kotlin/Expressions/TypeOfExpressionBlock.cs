using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    public class TypeOfExpressionBlock : AbstractKotlinEmitterBlock<TypeOfExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteType(Node.Type);
            Write(".class");
        }
    }
}
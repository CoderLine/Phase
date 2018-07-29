using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Expressions
{
    public class TypeOfExpressionBlock : AbstractJavaEmitterBlock<TypeOfExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteType(Node.Type);
            Write(".class");
        }
    }
}
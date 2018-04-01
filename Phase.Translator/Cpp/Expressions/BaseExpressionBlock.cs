using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    public class BaseExpressionBlock : AbstractCppEmitterBlock<BaseExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var baseType = Emitter.GetTypeInfo(Node, cancellationToken);
            Write(Emitter.GetTypeName(baseType.Type, true, true, CppEmitter.TypeNamePointerKind.NoPointer));
        }
    }
}
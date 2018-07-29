using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Java.Expressions
{
    public enum AutoCastMode
    {
        Default,
        AddParenthesis,
        SkipCast
    }
    public interface IAutoCastBlock { }
    public abstract class AutoCastBlockBase<T> : AbstractJavaEmitterBlock<T>, IAutoCastBlock
        where T : SyntaxNode
    {
        protected sealed override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            PushWriter();
            var mode = DoEmitWithoutCast(cancellationToken);
            var result = PopWriter();

            if (mode == AutoCastMode.SkipCast)
            {
                Write(result);
            }
            else
            {
                var typeInfo = Emitter.GetTypeInfo(Node, cancellationToken);
                WriteWithAutoCast(mode, typeInfo.ConvertedType, typeInfo.Type, result);
            }
        }


        protected abstract AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken));
    }
}

using System.Threading;
using System.Threading.Tasks;
using Phase.Translator.Haxe;

namespace Phase.Translator
{
    public abstract partial class AbstractEmitterBlock
    {
        protected IWriter Writer;

        public virtual void Emit(CancellationToken cancellationToken = default(CancellationToken))
        {
            BeginEmit(cancellationToken);
            DoEmit(cancellationToken);
            EndEmit(cancellationToken);
        }

        protected virtual void BeginEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
        }

        protected abstract void DoEmit(CancellationToken cancellationToken = default(CancellationToken));

        protected virtual void EndEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
        }
    }
}

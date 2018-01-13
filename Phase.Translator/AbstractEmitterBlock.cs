﻿using System.Threading;
using System.Threading.Tasks;
using Phase.Translator.Haxe;

namespace Phase.Translator
{
    public abstract partial class AbstractEmitterBlock
    {
        protected IWriter Writer;

        public virtual async Task EmitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await BeginEmitAsync(cancellationToken);
            await DoEmitAsync(cancellationToken);
            await EndEmitAsync(cancellationToken);
        }

        protected virtual async Task BeginEmitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
        }

        protected abstract Task DoEmitAsync(CancellationToken cancellationToken = default(CancellationToken));

        protected virtual async Task EndEmitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
        }
    }
}

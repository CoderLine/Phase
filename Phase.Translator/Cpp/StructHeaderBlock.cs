using System;
using System.Threading;

namespace Phase.Translator.Cpp
{
    class StructHeaderBlock : AbstractCppEmitterBlock
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException("Structs not yet supported");
        }
    }
}
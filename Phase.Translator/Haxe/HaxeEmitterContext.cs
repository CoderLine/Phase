using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NLog;

namespace Phase.Translator.Haxe
{
    public class HaxeEmitterContext
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public HaxeEmitter Emitter { get; }
        public PhaseType CurrentType { get; }
        public bool IsMethodInvocation { get; set; }
        public IWriter Writer { get; set; }

        public HaxeEmitterContext(HaxeEmitter emitter, PhaseType type)
        {
            Emitter = emitter;
            CurrentType = type;
            Writer = new InMemoryWriter();
        }

        public async Task<HaxeEmitterContext> EmitAsync(CancellationToken cancellationToken)
        {
            Log.Trace($"\tEmitting Type {CurrentType.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
            switch (CurrentType.Kind)
            {
                case PhaseTypeKind.Class:
                    var classBlock = new ClassBlock(this, (PhaseClass)CurrentType);
                    await classBlock.EmitAsync(cancellationToken);
                    break;
                case PhaseTypeKind.Struct:
                    var structBlock = new ClassBlock(this, (PhaseStruct)CurrentType);
                    await structBlock.EmitAsync(cancellationToken);
                    break;
                case PhaseTypeKind.Interface:
                    var interfaceBlock = new InterfaceBlock(this, (PhaseInterface)CurrentType);
                    await interfaceBlock.EmitAsync(cancellationToken);
                    break;
                case PhaseTypeKind.Enum:
                    var enumBlock = new EnumBlock(this, (PhaseEnum)CurrentType);
                    await enumBlock.EmitAsync(cancellationToken);
                    break;
                case PhaseTypeKind.Delegate:
                    var delegateBlock = new DelegateBlock(this, (PhaseDelegate)CurrentType);
                    await delegateBlock.EmitAsync(cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return this;
        }
    }
}

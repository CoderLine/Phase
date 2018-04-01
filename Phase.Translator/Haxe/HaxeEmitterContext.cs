using System;
using System.IO;
using System.Threading;
using NLog;

namespace Phase.Translator.Haxe
{
    public class HaxeEmitterContext : BaseEmitterContext<HaxeEmitter>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public override string FileName
        {
            get
            {
                var name = Emitter.GetTypeName(CurrentType.TypeSymbol);
                var p = name.IndexOf("<");
                if (p >= 0) name = name.Substring(0, p);
                return name.Replace('.', Path.DirectorySeparatorChar) + ".hx";
            }
        }

        public HaxeEmitterContext(HaxeEmitter emitter, PhaseType type)
            :base(emitter, type)
        {
        }

        public override void Emit(CancellationToken cancellationToken)
        {
            //Log.Trace($"\tEmitting Type {CurrentType.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
            switch (CurrentType.Kind)
            {
                case PhaseTypeKind.Class:
                    var classBlock = new ClassBlock(this);
                    classBlock.Emit(cancellationToken);
                    break;
                case PhaseTypeKind.Struct:
                    var structBlock = new ClassBlock(this);
                    structBlock.Emit(cancellationToken);
                    break;
                case PhaseTypeKind.Interface:
                    var interfaceBlock = new InterfaceBlock(this);
                    interfaceBlock.Emit(cancellationToken);
                    break;
                case PhaseTypeKind.Enum:
                    var enumBlock = new EnumBlock(this);
                    enumBlock.Emit(cancellationToken);
                    break;
                case PhaseTypeKind.Delegate:
                    var delegateBlock = new DelegateBlock(this);
                    delegateBlock.Emit(cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

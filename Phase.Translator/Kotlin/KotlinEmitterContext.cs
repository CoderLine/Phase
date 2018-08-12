using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using NLog;
using Phase.Translator.Utils;

namespace Phase.Translator.Kotlin
{
    public class KotlinEmitterContext : BaseEmitterContext<KotlinEmitter>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public override string FileName
        {
            get
            {
                var name = Emitter.GetTypeName(CurrentType.TypeSymbol);
                var p = name.IndexOf("<");
                if (p >= 0) name = name.Substring(0, p);
                name = name.Replace("?", "");
                return name.Replace('.', Path.DirectorySeparatorChar) + ".kt";
            }
        }

        public int RecursiveForeach { get; set; }

        public KotlinEmitterContext(KotlinEmitter emitter, PhaseType type)
            : base(emitter, type)
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

        public void WriteNestedType(INamedTypeSymbol member, CancellationToken cancellationToken)
        {
            if (Emitter.NestedTypes.TryGetValue(member, out var type))
            {
                switch (type.Kind)
                {
                    case PhaseTypeKind.Class:
                        var classBlock = new ClassBlock(type, this);
                        classBlock.EmitNested(cancellationToken);
                        break;
                    case PhaseTypeKind.Struct:
                        var structBlock = new ClassBlock(type, this);
                        structBlock.EmitNested(cancellationToken);
                        break;
                    case PhaseTypeKind.Interface:
                        var interfaceBlock = new InterfaceBlock(type, this);
                        interfaceBlock.EmitNested(cancellationToken);
                        break;
                    case PhaseTypeKind.Enum:
                        var enumBlock = new EnumBlock(type, this);
                        enumBlock.EmitNested(cancellationToken);
                        break;
                    case PhaseTypeKind.Delegate:
                        var delegateBlock = new DelegateBlock(type, this);
                        delegateBlock.EmitNested(cancellationToken);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}

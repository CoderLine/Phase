using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using NLog;
using Phase.Translator.Utils;

namespace Phase.Translator.TypeScript
{
    public class TypeScriptEmitterContext : BaseEmitterContext<TypeScriptEmitter>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public ConcurrentDictionary<ITypeSymbol, ImportInfo> ImportedTypes { get; }

        public override string FileName => Emitter.GetFileName(CurrentType.TypeSymbol, true);

        public class ImportInfo
        {
            public ITypeSymbol Type { get; set; }
        }

        public TypeScriptEmitterContext(TypeScriptEmitter emitter, PhaseType type)
            : base(emitter, type)
        {
            ImportedTypes = new ConcurrentDictionary<ITypeSymbol, ImportInfo>(SymbolEquivalenceComparer.Instance);
        }

        public void ImportType(ITypeSymbol baseType)
        {
            if (baseType.TypeKind == TypeKind.TypeParameter) return;
            if (baseType.OriginalDefinition.Equals(CurrentType.TypeSymbol)) return;
            if (Emitter.ShouldOmitImport(baseType)) return;            

            switch (baseType.SpecialType)
            {
                case SpecialType.System_Object:
                case SpecialType.System_Void:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                    return;
            }

            if (baseType is IDynamicTypeSymbol)
            {
                return;
            }

            if (baseType is IArrayTypeSymbol array)
            {
                ImportType(array.ElementType);
                return;
            }

            if (baseType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
            {
                if (!ImportedTypes.ContainsKey(baseType.OriginalDefinition))
                {
                    ImportedTypes[baseType.OriginalDefinition] = new ImportInfo
                    {
                        Type = baseType.OriginalDefinition,
                    };
                }
            }

            if (baseType is INamedTypeSymbol named && named.IsGenericType)
            {
                foreach (var namedTypeArgument in named.TypeArguments)
                {
                    ImportType(namedTypeArgument);
                }
            }
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
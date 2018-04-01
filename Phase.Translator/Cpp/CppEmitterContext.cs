using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Phase.Translator.Utils;

namespace Phase.Translator.Cpp
{
    public abstract class CppEmitterContext : BaseEmitterContext<CppEmitter>
    {
        public override string FileName
        {
            get
            {
                var name = Emitter.GetTypeName(CurrentType.TypeSymbol, false, true, CppEmitter.TypeNamePointerKind.NoPointer);
                var p = name.IndexOf("<");
                if (p >= 0) name = name.Substring(0, p);
                return name.Replace('.', Path.DirectorySeparatorChar).Replace("::", Path.DirectorySeparatorChar.ToString()) + FileExtension;
            }
        }

        public abstract string FileExtension { get; }
        public string PreviousAccessibility { get; set; }
        public ConcurrentDictionary<ITypeSymbol, ImportInfo> ImportedTypes { get; }
        public int RecursiveForeach { get; set; }
        public int RecursiveFinally { get; set; }
        public bool IsParameter { get; set; }

        protected CppEmitterContext(CppEmitter emitter, PhaseType type)
            :base(emitter, type)
        {
            ImportedTypes = new ConcurrentDictionary<ITypeSymbol, ImportInfo>(SymbolEquivalenceComparer.Instance);
        }

        public void ImportType(ITypeSymbol baseType, bool requiresInclude = false)
        {
            if (baseType.TypeKind == TypeKind.TypeParameter) return;
            if (baseType.OriginalDefinition.Equals(CurrentType.TypeSymbol)) return;
            
            if (baseType is IArrayTypeSymbol array)
            {
                ImportType(array.ElementType, requiresInclude);
                return;
            }

            if (ImportedTypes.TryGetValue(baseType.OriginalDefinition, out var info))
            {
                if (requiresInclude)
                {
                    info.RequiresInclude = true;
                }
            }
            else
            {
                ImportedTypes[baseType.OriginalDefinition] = new ImportInfo
                {
                    Type = baseType.OriginalDefinition,
                    RequiresInclude = requiresInclude
                };
            }
            
            if (baseType is INamedTypeSymbol named && named.IsGenericType)
            {
                foreach (var namedTypeArgument in named.TypeArguments)
                {
                    ImportType(namedTypeArgument, requiresInclude);
                }
            }
        }

        public class ImportInfo
        {
            public ITypeSymbol Type { get; set; }
            public bool RequiresInclude { get; set; }
        }
    }

    public class CppEmitterContext<TBlock> : CppEmitterContext
        where TBlock : AbstractCppEmitterBlock, new()
    {
        public override string FileExtension { get;}

        public CppEmitterContext(CppEmitter emitter, PhaseType type, string extension)
            :base(emitter, type)
        {
            FileExtension = extension;
        }

        public override void Emit(CancellationToken cancellationToken)
        {
            var block = new TBlock();
            block.Init(this);
            block.Emit(cancellationToken);
        }
    }
}

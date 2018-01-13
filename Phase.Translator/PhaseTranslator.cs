using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using NLog;
using Phase.Translator.Haxe;

namespace Phase.Translator
{
    public class PhaseTranslator
    {
        public PhaseCompiler Compiler { get; set; }
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public Project Project { get; private set; }
        public CSharpCompilation Compilation { get; private set; }
        public List<Tuple<ITypeSymbol, SemanticModel>> Types { get; private set; }
        public EmitResult Result { get; set; }
        public AttributeRegistry Attributes { get; private set; }

        public PhaseTranslator(PhaseCompiler compiler)
        {
            Compiler = compiler;
        }

        public async Task TranslateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await ReadProjectFileAsync(cancellationToken);
            if (await PrecompileAsync(cancellationToken))
            {
                if (await PreprocessAsync(cancellationToken))
                {
                    await EmitAsync(cancellationToken);
                }
            }
        }

        private async Task EmitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var types = LoadTypes();

            IEmitter emitter;
            switch (Compiler.Options.Language)
            {
                case PhaseLanguage.Haxe:
                    emitter = new HaxeEmitter(Compiler);
                    break;
                default:
                    Log.Error("Invalid compilation language");
                    throw new PhaseCompilerException("Invalid compilation language");
            }

            Log.Trace("Start Emitting");
            Result = await emitter.EmitAsync(Compilation, types, cancellationToken);
            Log.Trace("Emitting done");
        }

        private IEnumerable<PhaseType> LoadTypes(CancellationToken cancellationToken = default(CancellationToken))
        {
            Log.Trace("Loading Types");

            var walker = new TypeLoadingWalker(Compilation);

            foreach (var syntaxTree in Compilation.SyntaxTrees)
            {
                walker.Visit(syntaxTree, cancellationToken);
            }

            Log.Trace("Loading Types done");
            return walker.PhaseTypes.Values;
        }

        public class TypeLoadingWalker : CSharpSyntaxWalker
        {
            private readonly CSharpCompilation _compilation;
            private SemanticModel _currentSemanticModel;
            public Dictionary<ISymbol, PhaseType> PhaseTypes { get; private set; }

            public TypeLoadingWalker(CSharpCompilation compilation)
            {
                _compilation = compilation;
                PhaseTypes = new Dictionary<ISymbol, PhaseType>();
            }

            public void Visit(SyntaxTree tree, CancellationToken cancellationToken = default(CancellationToken))
            {
                _currentSemanticModel = _compilation.GetSemanticModel(tree);
                Visit(tree.GetRoot(cancellationToken));
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                AddType(new PhaseClass(node, _currentSemanticModel));
                base.VisitClassDeclaration(node);
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                AddType(new PhaseStruct(node, _currentSemanticModel));
                base.VisitStructDeclaration(node);
            }

            public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
            {
                AddType(new PhaseEnum(node, _currentSemanticModel));
                base.VisitEnumDeclaration(node);
            }

            public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            {
                AddType(new PhaseInterface(node, _currentSemanticModel));
                base.VisitInterfaceDeclaration(node);
            }

            public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
            {
                AddType(new PhaseDelegate(node, _currentSemanticModel));
                base.VisitDelegateDeclaration(node);
            }

            private void AddType(PhaseType type)
            {
                if (PhaseTypes.TryGetValue(type.TypeSymbol, out var existing))
                {
                    existing.Merge(type);
                }
                else
                {
                    PhaseTypes[type.TypeSymbol] = type;
                }
            }
        }

        private async Task<bool> PreprocessAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Log.Trace("Start project preprocessing");

            var hasError = false;
            try
            {
                Log.Trace("Loading attributes");
                await RegisterAttributesAsync(cancellationToken);
            }
            catch (Exception e)
            {
                Log.Error(e, "Attribute loading failed");
                throw new PhaseCompilerException("Attribute loading failed", e);
            }

            var diagnostics = Compilation.GetDiagnostics();
            foreach (var diagnostic in diagnostics)
            {
                switch (diagnostic.Severity)
                {
                    case DiagnosticSeverity.Hidden:
                        break;
                    case DiagnosticSeverity.Info:
                        Log.Info(diagnostic.GetMessage(CultureInfo.InvariantCulture));
                        break;
                    case DiagnosticSeverity.Warning:
                        Log.Warn(diagnostic.GetMessage(CultureInfo.InvariantCulture));
                        break;
                    case DiagnosticSeverity.Error:
                        Log.Error(diagnostic.GetMessage(CultureInfo.InvariantCulture));
                        hasError = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            Log.Trace("Finished project preprocessing");
            return !hasError;
        }

        private async Task RegisterAttributesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Log.Trace("Loading attributes from project");
            var attributeLoader = new AttributeLoader(Compilation);
            await attributeLoader.LoadAsync(cancellationToken);
            Attributes = attributeLoader.Attributes;
            Compilation = attributeLoader.Compilation;
            Log.Trace("Attributes from project loaded");
        }

        private async Task<bool> PrecompileAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Log.Trace("Start project compilation");
            var hasError = false;
            try
            {
                Compilation = (CSharpCompilation)await Project.GetCompilationAsync(cancellationToken);
                Log.Trace("Project compiled");

                var diagnostics = Compilation.GetDiagnostics();
                foreach (var diagnostic in diagnostics)
                {
                    switch (diagnostic.Severity)
                    {
                        case DiagnosticSeverity.Hidden:
                            break;
                        case DiagnosticSeverity.Info:
                            Log.Info(diagnostic.GetMessage(CultureInfo.InvariantCulture));
                            break;
                        case DiagnosticSeverity.Warning:
                            Log.Warn(diagnostic.GetMessage(CultureInfo.InvariantCulture));
                            break;
                        case DiagnosticSeverity.Error:
                            Log.Error(diagnostic.GetMessage(CultureInfo.InvariantCulture));
                            hasError = true;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                foreach (var reference in Compilation.References)
                {
                    if (reference is PortableExecutableReference peReference)
                    {
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Project compilation failed");
                throw new PhaseCompilerException("Project compilation failed", e);
            }

            Log.Trace("Finished project compilation");

            return !hasError;
        }

        private async Task ReadProjectFileAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Log.Trace("Begin reading project file");

            try
            {
                var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
                {
                    ["Configuration"] = Compiler.Input.Configuration,
                    ["Platform"] = Compiler.Input.Platform
                });
                workspace.WorkspaceFailed += (sender, args) => {; };
                Project = await workspace.OpenProjectAsync(Compiler.Input.ProjectFile, cancellationToken);
                if (!Project.SupportsCompilation)
                {
                    throw new PhaseCompilerException("Unsupported project type, does not support compilation");
                }
            }
            catch (PhaseCompilerException)
            {
                throw;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to open project");
                throw new PhaseCompilerException("Failed to open project", e);
            }

            Log.Trace("Finished reading project file");
        }
    }
}

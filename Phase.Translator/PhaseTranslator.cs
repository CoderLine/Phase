using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using Phase.Translator.Cpp;
using Phase.Translator.Haxe;
using Phase.Translator.Java;
using Phase.Translator.Utils;

namespace Phase.Translator
{
    public class PhaseTranslator
    {
        public PhaseCompiler Compiler { get; set; }
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
            using (new LogHelper("translation", Log, 1))
            {
                await PrecompileAsync(cancellationToken);
                await PreprocessAsync(cancellationToken);
                await EmitAsync(cancellationToken);
            }
        }

        private async Task EmitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var types = LoadTypes();

            using (new LogHelper("emitting", Log, 2))
            {
                IEmitter emitter;
                switch (Compiler.Options.Language)
                {
                    case PhaseLanguage.Haxe:
                        emitter = new HaxeEmitter(Compiler);
                        break;
                    case PhaseLanguage.Cpp:
                        emitter = new CppEmitter(Compiler);
                        break;
                    case PhaseLanguage.Java:
                        emitter = new JavaEmitter(Compiler);
                        break;
                    default:
                        Log.Error("Invalid compilation language");
                        throw new PhaseCompilerException("Invalid compilation language");
                }

                Result = await emitter.EmitAsync(Compilation, types, cancellationToken);
            }
        }

        private IEnumerable<PhaseType> LoadTypes(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new LogHelper("loading types", Log, 2))
            {
                var walker = new TypeLoadingWalker(Compilation);

                foreach (var syntaxTree in Compilation.SyntaxTrees)
                {
                    walker.Visit(syntaxTree, cancellationToken);
                }

                return walker.PhaseTypes.Values;
            }
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

        private async Task PreprocessAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new LogHelper("project preprocessing", Log, 2))
            {
                var hasError = false;

                try
                {
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
                            Log.Info(CultureInfo.InvariantCulture, diagnostic);
                            break;
                        case DiagnosticSeverity.Warning:
                            Log.Warn(CultureInfo.InvariantCulture, diagnostic);
                            break;
                        case DiagnosticSeverity.Error:
                            Log.Error(CultureInfo.InvariantCulture, diagnostic);
                            hasError = true;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (hasError)
                {
                    throw new PhaseCompilerException("Compilation failed with errors, see output for details");
                }
            }
        }

        private async Task RegisterAttributesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new LogHelper("loading attributes from project", Log, 3))
            {
                var attributeLoader = new AttributeLoader(Compiler.Options.Language, Compilation);
                await attributeLoader.LoadAsync(cancellationToken);
                Attributes = attributeLoader.Attributes;
                Compilation = attributeLoader.Compilation;
            }
        }

        private async Task PrecompileAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new LogHelper("project compilation", Log, 2))
            {
                try
                {
                    if (Compiler.Input.SourceFiles == null)
                    {
                        var compiler = new MSBuildProjectCompiler(new Dictionary<string, string>
                        {
                            ["Configuration"] = Compiler.Input.Configuration,
                            ["Platform"] = Compiler.Input.Platform,
                        });
                        Compilation =
                            (CSharpCompilation)await compiler.BuildAsync(Compiler.Input.ProjectFile,
                                cancellationToken);
                    }
                    else
                    {
                        Compilation = MSBuildProjectCompiler.CSharpCompilationHost.Compile(
                            Path.GetDirectoryName(Compiler.Input.ProjectFile),
                            Compiler.Input.CompilationOptions,
                            Compiler.Input.ParseOptions,
                            Compiler.Input.SourceFiles,
                            Compiler.Input.ReferencedAssemblies,
                            cancellationToken);
                        Log.Info("Via Source files: '" + Compiler.Input.CompilationOptions.ModuleName + "'" );
                    }

                    Log.Trace("Project compiled");
                }
                catch (Exception e)
                {
                    Log.Error(e, "Project compilation failed");
                    throw new PhaseCompilerException("Project compilation failed", e);
                }
            }
        }
    }
}

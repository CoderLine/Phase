using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NLog;
using NLog.Config;
using NLog.Targets;
using Phase.Translator;
using Phase.Translator.Utils;

namespace Phase.MsBuild
{
    public class Phase : Task, ICancelableTask
    {
        public string DefineConstants
        {
            set;
            get;
        }

        public string DisabledWarnings
        {
            set;
            get;
        }

        public string MainEntryPoint
        {
            set;
            get;
        }

        public string ModuleAssemblyName
        {
            set;
            get;
        }

        [Required]
        public string Configuration
        {
            set;
            get;
        }

        [Required]
        public string Platform
        {
            set;
            get;
        }

        [Required]
        public ITaskItem[] References
        {
            set;
            get;
        }

        [Required]
        public ITaskItem[] Sources
        {
            set;
            get;
        }

        public string TargetType
        {
            set;
            get;
        }

        public bool TreatWarningsAsErrors
        {
            set;
            get;
        }

        public int WarningLevel
        {
            set;
            get;
        }

        public string WarningsAsErrors
        {
            set;
            get;
        }

        public string WarningsNotAsErrors
        {
            set;
            get;
        }

        [Required]
        public string ProjectFile
        {
            set;
            get;
        }


        static Phase()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name.StartsWith("System.Collections.Immutable"))
                {
                    return typeof(ImmutableArray).Assembly;
                }
                return null;
            };
        }

        private CancellationTokenSource _cancellationTokenSource;

        public override bool Execute()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var directory = Path.GetDirectoryName(ProjectFile);

            var arguments = new List<string>();
            arguments.Add("/define:\"" + DefineConstants + "\"");
            arguments.Add("/nowarn:\"" + DisabledWarnings + "\"");
            if (!string.IsNullOrEmpty(MainEntryPoint))
            {
                arguments.Add("/main:\"" + MainEntryPoint + "\"");
            }
            arguments.Add("/target:" + TargetType);
            arguments.Add("/moduleassemblyname:\"" + ModuleAssemblyName + "\"");
            if (TreatWarningsAsErrors)
            {
                arguments.Add("/warnaserror");
            }
            arguments.Add("/warn:\"" + WarningLevel + "\"");
            arguments.Add("/warnaserror+:\"" + WarningsAsErrors + "\"");
            arguments.Add("/warnaserror-:\"" + WarningsNotAsErrors + "\"");
            foreach (var mr in References)
            {
                var filePath = Path.Combine(directory, mr.ItemSpec);

                var aliases = GetAliases(mr);
                if (aliases.IsDefaultOrEmpty)
                {
                    arguments.Add("/reference:\"" + filePath + "\"");
                }
                else
                {
                    foreach (var alias in aliases)
                    {
                        arguments.Add("/reference:" + alias + "=\"" + filePath + "\"");
                    }
                }
            }


            try
            {
                ImmutableArray.Create(1, 2, 3);
                var config = new LoggingConfiguration();
                config.AddTarget("msbuild", new MSBuildTarget(Log)
                {
                    Layout = "${longdate} ${level} ${callsite} - ${message} ${exception:format=ToString}"
                });
                config.AddRule(LogLevel.Trace, LogLevel.Fatal, "msbuild");
                LogManager.Configuration = config;

                var args = CSharpCommandLineParser.Default.Parse(arguments, directory, RuntimeEnvironment.GetRuntimeDirectory());

                var resolver = new MetadataFileReferenceResolver(directory);
                var references = args.ResolveMetadataReferences(resolver);

                var input = new PhaseCompilerInput
                {
                    ProjectFile = ProjectFile,
                    CompilationOptions = args.CompilationOptions,
                    ParseOptions = args.ParseOptions.WithDocumentationMode(DocumentationMode.Parse),
                    SourceFiles = Sources.Select(s => Path.Combine(directory, s.ItemSpec)).ToArray(),
                    ReferencedAssemblies = references,
                    Platform = Platform,
                    Configuration = Configuration
                };

                var compiler = new PhaseCompiler(input);
                compiler.CompileAsync(_cancellationTokenSource.Token).Wait();
                return true;
            }
            catch (Exception e)
            {
                Log.LogError(e.Message);
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                return false;
            }
        }

        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        private ImmutableArray<string> GetAliases(ITaskItem item)
        {
            var aliasesText = item.GetMetadata("Aliases");

            if (string.IsNullOrEmpty(aliasesText))
            {
                return ImmutableArray<string>.Empty;
            }

            return ImmutableArray.CreateRange(aliasesText.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
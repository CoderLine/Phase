using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NLog;
using NLog.Config;
using Phase.Translator;
using Phase.Translator.Utils;

namespace Phase.MsBuild
{
    public class Phase : Task
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

        public string Platform
        {
            set;
            get;
        }

        public ITaskItem[] References
        {
            set;
            get;
        }

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


        public override bool Execute()
        {
            var projectFile = BuildEngine.ProjectFileOfTaskNode;
            var directory = Path.GetDirectoryName(projectFile);

            var arguments = new List<string>();
            arguments.Add("/define:\"" + DefineConstants + "\"");
            arguments.Add("/nowarn:\"" + DisabledWarnings + "\"");
            arguments.Add("/main:\"" + MainEntryPoint + "\"");
            arguments.Add("/target:\"" + MainEntryPoint + "\"");
            arguments.Add("/moduleassemblyname:\"" + TargetType + "\"");
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
                LogManager.Configuration = new LoggingConfiguration();
                LogManager.Configuration.AddTarget("msbuild", new MSBuildTarget(Log));
                LogManager.Configuration.AddRule(LogLevel.Trace, LogLevel.Off, "msbuild");

                var args = CSharpCommandLineParser.Default.Parse(arguments, directory, RuntimeEnvironment.GetRuntimeDirectory());

                var resolver = new MetadataFileReferenceResolver(directory);
                var references = args.ResolveMetadataReferences(resolver);

                var input = new PhaseCompilerInput
                {
                    ProjectFile = projectFile,
                    CompilationOptions = args.CompilationOptions,
                    ParseOptions = args.ParseOptions,
                    SourceFiles = Sources.Select(s => Path.Combine(directory, s.ItemSpec)).ToArray(),
                    ReferencedAssemblies = references
                };

                var compiler = new PhaseCompiler(input);
                compiler.Compile().Wait();
                return true;
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e, true);
                return false;
            }
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
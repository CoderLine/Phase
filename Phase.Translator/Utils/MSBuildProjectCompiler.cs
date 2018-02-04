// The code of this class is heavily based on the Roslyn Workspaces.Desktop source code 
// https://github.com/dotnet/roslyn/
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using NLog;
using Roslyn.Utilities;
using HostServices = Microsoft.Build.Execution.HostServices;
using ILogger = Microsoft.Build.Framework.ILogger;

namespace Phase.Translator.Utils
{
    class MSBuildProjectCompiler
    {
        private static readonly NLog.Logger Log = LogManager.GetCurrentClassLogger();
        private readonly IDictionary<string, string> _properties;

        public MSBuildProjectCompiler(IDictionary<string, string> properties = null)
        {
            _properties = properties;
        }

        public Task<Compilation> BuildAsync(string inputProjectFile, CancellationToken cancellationToken = default(CancellationToken))
        {
            inputProjectFile = Path.GetFullPath(inputProjectFile);

            var extension = Path.GetExtension(inputProjectFile);
            ICompilationHost host;
            switch (extension)
            {
                case ".csproj":
                    host = new CSharpCompilationHost(inputProjectFile);
                    break;
                //case ".vbproj":
                //    host = new VisualBasicCompilationHost();
                //    break;
                default:
                    throw new InvalidOperationException(
                        $"Cannot compile project '{inputProjectFile}' because the file extension '{extension}' is not associated with a language");
            }
            return BuildAsync(inputProjectFile, host, cancellationToken);
        }


        private async Task<Compilation> BuildAsync(string inputProjectFile, ICompilationHost compilationHost, CancellationToken cancellationToken = default(CancellationToken))
        {
            var hostServices = new HostServices();
            hostServices.RegisterHostObject(inputProjectFile, "CoreCompile", compilationHost.TaskName, compilationHost);

            var properties = new Dictionary<string, string>(_properties ?? ImmutableDictionary<string, string>.Empty)
            {
                ["DesignTimeBuild"] = "true", // this will tell msbuild to not build the dependent projects
                ["BuildingInsideVisualStudio"] = "true" // this will force CoreCompile task to execute even if all inputs and outputs are up to date
            };

            var errorLogger = new Logger() { Verbosity = LoggerVerbosity.Normal };

            var buildParameters = new BuildParameters
            {
                GlobalProperties = properties,
                Loggers = new ILogger[] { errorLogger }
            };

            var buildRequestData = new BuildRequestData(inputProjectFile, properties, null, new string[] { "Compile" }, hostServices);
            var sw = new Stopwatch();
            sw.Start();
            Log.Trace("Begin MSBuild");
            var result = await BuildAsync(buildParameters, buildRequestData, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            sw.Stop();
            Log.Trace($"Finish MSBuild {sw.Elapsed.TotalMilliseconds}ms");
            if (result.OverallResult == BuildResultCode.Failure)
            {
                throw result.Exception ?? new InvalidOperationException("Error during project compilation");
            }

            return compilationHost.Compile(cancellationToken);
        }

        private static readonly SemaphoreSlim BuildManagerLock = new SemaphoreSlim(initialCount: 1);
        private async Task<BuildResult> BuildAsync(BuildParameters parameters, BuildRequestData requestData, CancellationToken cancellationToken)
        {
            // only allow one build to use the default build manager at a time
            using (await BuildManagerLock.DisposableWaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
            {
                return await BuildAsync(BuildManager.DefaultBuildManager, parameters, requestData, cancellationToken);//.ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private static Task<BuildResult> BuildAsync(BuildManager buildManager, BuildParameters parameters, BuildRequestData requestData, CancellationToken cancellationToken)
        {
            //return buildManager.Build(parameters, requestData);

            var taskSource = new TaskCompletionSource<BuildResult>();
            buildManager.BeginBuild(parameters);

            // enable cancellation of build
            var registration = default(CancellationTokenRegistration);
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    try
                    {
                        buildManager.CancelAllSubmissions();
                        buildManager.EndBuild();
                        registration.Dispose();
                    }
                    finally
                    {
                        taskSource.TrySetCanceled();
                    }
                });
            }

            // execute build async
            try
            {
                buildManager.PendBuildRequest(requestData).ExecuteAsync(sub =>
                {
                    // when finished
                    try
                    {
                        var result = sub.BuildResult;
                        buildManager.EndBuild();
                        registration.Dispose();
                        taskSource.TrySetResult(result);
                    }
                    catch (Exception e)
                    {
                        taskSource.TrySetException(e);
                    }
                }, null);
            }
            catch (Exception e)
            {
                taskSource.SetException(e);
            }

            return taskSource.Task;
        }

        private class Logger : ILogger
        {
            private static readonly NLog.Logger Log = LogManager.GetCurrentClassLogger();
            private IEventSource _eventSource;

            public string Parameters { get; set; }
            public LoggerVerbosity Verbosity { get; set; }

            private Dictionary<string, Stopwatch> _targetTimes;
            private Dictionary<string, Stopwatch> _taskTimes;
            private Dictionary<string, Stopwatch> _projectTimes;
            private Stopwatch _buildStart;

            public void Initialize(IEventSource eventSource)
            {
                _eventSource = eventSource;
                _eventSource.BuildStarted += OnBuildStarted;
                _eventSource.ErrorRaised += OnErrorRaised;
                _eventSource.TargetStarted += OnTargetStarted;
                _eventSource.TargetFinished += OnTargetFinished;
                _eventSource.TaskStarted += OnTaskStarted;
                _eventSource.TaskFinished += OnTaskFinished;
                _eventSource.BuildFinished += OnBuildFinished;
                _eventSource.ProjectStarted += OnProjectStarted;
                _eventSource.ProjectFinished += OnProjectFinished;
                _eventSource.MessageRaised += OnMessage;
                _eventSource.StatusEventRaised += OnStatus;
                _targetTimes = new Dictionary<string, Stopwatch>();
                _taskTimes = new Dictionary<string, Stopwatch>();
                _projectTimes = new Dictionary<string, Stopwatch>();
            }

            private void OnStatus(object sender, BuildStatusEventArgs e)
            {
                //Log.Trace($"Status: {e.Message}");
            }

            private void OnMessage(object sender, BuildMessageEventArgs e)
            {
                //Log.Trace($"Message: {e.Message}");
            }

            private void OnTaskFinished(object sender, TaskFinishedEventArgs e)
            {
                //_taskTimes[e.TaskName].Stop();
                //Log.Trace($"Task '{e.TaskName}' Finished in {_taskTimes[e.TaskName].Elapsed.TotalMilliseconds}ms");
            }

            private void OnTaskStarted(object sender, TaskStartedEventArgs e)
            {
                //Log.Trace($"Task '{e.TaskName}' Started");
                //var sw = new Stopwatch();
                //_taskTimes[e.TaskName] = sw;
                //sw.Start();
            }

            private void OnProjectFinished(object sender, ProjectFinishedEventArgs e)
            {
                //_projectTimes[e.ProjectFile].Stop();
                //Log.Trace($"Preparing project {Path.GetFileName(e.ProjectFile)} finished in {_projectTimes[e.ProjectFile].Elapsed.TotalMilliseconds}ms");
            }

            private void OnProjectStarted(object sender, ProjectStartedEventArgs e)
            {
                //Log.Trace($"Preparing project {Path.GetFileName(e.ProjectFile)} started");
                //var sw = new Stopwatch();
                //_projectTimes[e.ProjectFile] = sw;
                //sw.Start();
            }

            private void OnBuildFinished(object sender, BuildFinishedEventArgs e)
            {
                _buildStart.Stop();
                Log.Trace($"C# compile preparation finished {_buildStart.Elapsed.TotalMilliseconds}ms");
            }

            private void OnBuildStarted(object sender, BuildStartedEventArgs e)
            {
                Log.Trace($"C# compile preparation started");
                _buildStart = new Stopwatch();
                _buildStart.Start();
            }

            private void OnTargetFinished(object sender, TargetFinishedEventArgs e)
            {
                //_targetTimes[e.TargetName].Stop();
                //Log.Trace($"'{e.TargetName}' Finished in {_targetTimes[e.TargetName].Elapsed.TotalMilliseconds}ms");
            }

            private void OnTargetStarted(object sender, TargetStartedEventArgs e)
            {
                //Log.Trace($"'{e.TargetName}' Started");
                //var sw = new Stopwatch();
                //_targetTimes[e.TargetName] = sw;
                //sw.Start();
            }

            private void OnErrorRaised(object sender, BuildErrorEventArgs e)
            {
                Log.Error($"Error during compilation {e.File}: ({e.LineNumber}, {e.ColumnNumber}): {e.Message}");
            }

            public void Shutdown()
            {
                if (_eventSource != null)
                {
                    _eventSource.BuildStarted -= OnBuildStarted;
                    _eventSource.ErrorRaised -= OnErrorRaised;
                    _eventSource.TargetStarted -= OnTargetStarted;
                    _eventSource.TargetFinished -= OnTargetFinished;
                    _eventSource.BuildFinished -= OnBuildFinished;
                    _eventSource.ProjectStarted -= OnProjectStarted;
                    _eventSource.ProjectFinished -= OnProjectFinished;
                }
            }
        }


        public interface ICompilationHost : ITaskHost
        {
            string TaskName { get; }

            Compilation Compile(CancellationToken cancellationToken);
        }

        public class CSharpCompilationHost : ICompilationHost, ICscHostObject4
        {
            private static readonly NLog.Logger Log = LogManager.GetCurrentClassLogger();

            private readonly string _baseDirectory;

            private bool _emitDebugInfo;
            private string _debugType;

            private string _targetType;
            private string _platform;

            public string TaskName => "Csc";

            public List<string> CommandLineArgs { get; }

            internal string[] Sources { get; private set; }
            internal string[] AdditionalSources { get; private set; }

            public CSharpCompilationHost(string projectFile)
            {
                _baseDirectory = Path.GetDirectoryName(projectFile);

                CommandLineArgs = new List<string>();
            }

            public bool IsDesignTime() => true;
            public bool IsUpToDate() => false;

            public Compilation Compile(CancellationToken cancellationToken)
            {
                var args = CSharpCommandLineParser.Default.Parse(CommandLineArgs, _baseDirectory,
                    RuntimeEnvironment.GetRuntimeDirectory());

                var resolver = new MetadataFileReferenceResolver(_baseDirectory);
                var references = args.ResolveMetadataReferences(resolver);

                return Compile(_baseDirectory, args.CompilationOptions, args.ParseOptions, Sources, references,
                    cancellationToken);
            }

            public static CSharpCompilation Compile(string baseDirectory, CSharpCompilationOptions compilationOptions, CSharpParseOptions parseOptions, string[] sources, IEnumerable<MetadataReference> references, CancellationToken cancellationToken)
            {
                Log.Trace("Parsing Source Files");
                var trees = new SyntaxTree[sources.Length];
                var parseErrors = new ConcurrentBag<Exception>();

                parseOptions = parseOptions
                    .WithDocumentationMode(DocumentationMode.Parse)
                    .WithPreprocessorSymbols(
                        parseOptions.PreprocessorSymbolNames.Concat(new[] {PhaseCompiler.Preprocessor}));

                Parallel.For(0, sources.Length, i =>
                {
                    try
                    {
                        using (var s = new FileStream(sources[i], FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
                            FileOptions.Asynchronous))
                        {
                            trees[i] = CSharpSyntaxTree.ParseText(SourceText.From(s),
                                parseOptions,
                                sources[i]);
                        }
                    }
                    catch (Exception e)
                    {
                        parseErrors.Add(e);
                    }
                });

                Log.Trace("Parsing Source Files completed");

                if (parseErrors.Count > 0)
                {
                    throw new AggregateException(parseErrors);
                }

                return CSharpCompilation.Create(
                    compilationOptions.ModuleName,
                    trees,
                    references,
                    options: compilationOptions
                );
            }

            bool ICscHostObject.Compile()
            {
                return true;
            }

            public void BeginInitialization()
            {
            }

            public bool EndInitialization(out string errorMessage, out int errorCode)
            {
                errorMessage = string.Empty;
                errorCode = 0;

                if (_emitDebugInfo)
                {
                    if (string.Equals(_debugType, "none", StringComparison.OrdinalIgnoreCase))
                    {
                        // does this mean not debug???
                        CommandLineArgs.Add("/debug");
                    }
                    else if (string.Equals(_debugType, "pdbonly", StringComparison.OrdinalIgnoreCase))
                    {
                        CommandLineArgs.Add("/debug:pdbonly");
                    }
                    else if (string.Equals(_debugType, "full", StringComparison.OrdinalIgnoreCase))
                    {
                        CommandLineArgs.Add("/debug:full");
                    }
                    else if (string.Equals(_debugType, "portable", StringComparison.OrdinalIgnoreCase))
                    {
                        CommandLineArgs.Add("/debug:portable");
                    }
                    else if (string.Equals(_debugType, "embedded", StringComparison.OrdinalIgnoreCase))
                    {
                        CommandLineArgs.Add("/debug:embedded");
                    }
                }

                if (!string.IsNullOrWhiteSpace(_platform))
                {
                    if (string.Equals("anycpu32bitpreferred", _platform, StringComparison.InvariantCultureIgnoreCase)
                        && (string.Equals("library", _targetType, StringComparison.InvariantCultureIgnoreCase)
                            || string.Equals("module", _targetType, StringComparison.InvariantCultureIgnoreCase)
                            || string.Equals("winmdobj", _targetType, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        _platform = "anycpu";
                    }

                    CommandLineArgs.Add("/platform:" + _platform);
                }

                return true;
            }

            public bool SetHighEntropyVA(bool highEntropyVA)
            {
                if (highEntropyVA)
                {
                    CommandLineArgs.Add("/highentropyva");
                }

                return true;
            }

            public bool SetSubsystemVersion(string subsystemVersion)
            {
                if (!string.IsNullOrWhiteSpace(subsystemVersion))
                {
                    CommandLineArgs.Add("/subsystemversion:" + subsystemVersion);
                }

                return true;
            }

            public bool SetApplicationConfiguration(string applicationConfiguration)
            {
                if (!string.IsNullOrWhiteSpace(applicationConfiguration))
                {
                    CommandLineArgs.Add("/appconfig:" + applicationConfiguration);
                }

                return true;
            }

            public bool SetWin32Manifest(string win32Manifest)
            {
                if (!string.IsNullOrWhiteSpace(win32Manifest))
                {
                    CommandLineArgs.Add("/win32manifest:\"" + win32Manifest + "\"");
                }

                return true;
            }


            public bool SetAdditionalLibPaths(string[] additionalLibPaths)
            {
                if (additionalLibPaths != null && additionalLibPaths.Length > 0)
                {
                    CommandLineArgs.Add("/lib:\"" + string.Join(";", additionalLibPaths) + "\"");
                }
                return true;
            }

            public bool SetAddModules(string[] addModules)
            {
                if (addModules != null && addModules.Length > 0)
                {
                    CommandLineArgs.Add("/addmodule:\"" + string.Join(";", addModules) + "\"");
                }

                return true;
            }

            public bool SetAllowUnsafeBlocks(bool allowUnsafeBlocks)
            {
                if (allowUnsafeBlocks)
                {
                    CommandLineArgs.Add("/unsafe");
                }

                return true;
            }

            public bool SetBaseAddress(string baseAddress)
            {
                if (!string.IsNullOrWhiteSpace(baseAddress))
                {
                    CommandLineArgs.Add("/baseaddress:" + baseAddress);
                }

                return true;
            }

            public bool SetCheckForOverflowUnderflow(bool checkForOverflowUnderflow)
            {
                if (checkForOverflowUnderflow)
                {
                    CommandLineArgs.Add("/checked");
                }

                return true;
            }

            public bool SetCodePage(int codePage)
            {
                if (codePage != 0)
                {
                    CommandLineArgs.Add("/codepage:" + codePage);
                }

                return true;
            }

            public bool SetDebugType(string debugType)
            {
                _debugType = debugType;
                return true;
            }

            public bool SetDefineConstants(string defineConstants)
            {
                if (!string.IsNullOrWhiteSpace(defineConstants))
                {
                    CommandLineArgs.Add("/define:" + defineConstants);
                }

                return true;
            }

            public bool SetDelaySign(bool delaySignExplicitlySet, bool delaySign)
            {
                if (delaySignExplicitlySet)
                {
                    CommandLineArgs.Add("/delaysign" + (delaySign ? "+" : "-"));
                }

                return true;
            }

            public bool SetDisabledWarnings(string disabledWarnings)
            {
                if (!string.IsNullOrWhiteSpace(disabledWarnings))
                {
                    CommandLineArgs.Add("/nowarn:" + disabledWarnings);
                }

                return true;
            }

            public bool SetDocumentationFile(string documentationFile)
            {
                if (!string.IsNullOrWhiteSpace(documentationFile))
                {
                    CommandLineArgs.Add("/doc:\"" + documentationFile + "\"");
                }

                return true;
            }

            public bool SetEmitDebugInformation(bool emitDebugInformation)
            {
                _emitDebugInfo = emitDebugInformation;
                return true;
            }

            public bool SetErrorReport(string errorReport)
            {
                if (!string.IsNullOrWhiteSpace(errorReport))
                {
                    CommandLineArgs.Add("/errorreport:" + errorReport.ToLower());
                }

                return true;
            }

            public bool SetFileAlignment(int fileAlignment)
            {
                CommandLineArgs.Add("/filealign:" + fileAlignment);
                return true;
            }

            public bool SetGenerateFullPaths(bool generateFullPaths)
            {
                if (generateFullPaths)
                {
                    CommandLineArgs.Add("/fullpaths");
                }

                return true;
            }

            public bool SetKeyContainer(string keyContainer)
            {
                if (!string.IsNullOrWhiteSpace(keyContainer))
                {
                    CommandLineArgs.Add("/keycontainer:\"" + keyContainer + "\"");
                }

                return true;
            }

            public bool SetKeyFile(string keyFile)
            {
                if (!string.IsNullOrWhiteSpace(keyFile))
                {
                    // keyFile = FileUtilities.ResolveRelativePath(keyFile, this.ProjectDirectory);
                    CommandLineArgs.Add("/keyfile:\"" + keyFile + "\"");
                }

                return true;
            }

            public bool SetLangVersion(string langVersion)
            {
                if (!string.IsNullOrWhiteSpace(langVersion))
                {
                    CommandLineArgs.Add("/langversion:" + langVersion);
                }

                return true;
            }

            public bool SetLinkResources(ITaskItem[] linkResources)
            {
                if (linkResources != null && linkResources.Length > 0)
                {
                    foreach (var lr in linkResources)
                    {
                        CommandLineArgs.Add("/linkresource:\"" + GetDocumentFilePath(lr) + "\"");
                    }
                }

                return true;
            }

            public bool SetMainEntryPoint(string targetType, string mainEntryPoint)
            {
                if (!string.IsNullOrWhiteSpace(mainEntryPoint))
                {
                    CommandLineArgs.Add("/main:\"" + mainEntryPoint + "\"");
                }

                return true;
            }

            public bool SetModuleAssemblyName(string moduleAssemblyName)
            {
                if (!string.IsNullOrWhiteSpace(moduleAssemblyName))
                {
                    CommandLineArgs.Add("/moduleassemblyname:\"" + moduleAssemblyName + "\"");
                }

                return true;
            }

            public bool SetNoConfig(bool noConfig)
            {
                if (noConfig)
                {
                    CommandLineArgs.Add("/noconfig");
                }

                return true;
            }

            public bool SetNoStandardLib(bool noStandardLib)
            {
                if (noStandardLib)
                {
                    CommandLineArgs.Add("/nostdlib");
                }

                return true;
            }

            public bool SetOptimize(bool optimize)
            {
                if (optimize)
                {
                    CommandLineArgs.Add("/optimize");
                }

                return true;
            }

            public bool SetOutputAssembly(string outputAssembly)
            {
                CommandLineArgs.Add("/out:\"" + outputAssembly + "\"");
                return true;
            }

            public bool SetPdbFile(string pdbFile)
            {
                if (!string.IsNullOrWhiteSpace(pdbFile))
                {
                    CommandLineArgs.Add($"/pdb:\"{pdbFile}\"");
                }

                return true;
            }

            public bool SetPlatform(string platform)
            {
                _platform = platform;
                return true;
            }

            public bool SetPlatformWith32BitPreference(string platformWith32BitPreference)
            {
                SetPlatform(platformWith32BitPreference);
                return true;
            }

            public bool SetReferences(ITaskItem[] references)
            {
                if (references != null)
                {
                    foreach (var mr in references)
                    {
                        var filePath = GetDocumentFilePath(mr);

                        var aliases = GetAliases(mr);
                        if (aliases.IsDefaultOrEmpty)
                        {
                            CommandLineArgs.Add("/reference:\"" + filePath + "\"");
                        }
                        else
                        {
                            foreach (var alias in aliases)
                            {
                                CommandLineArgs.Add("/reference:" + alias + "=\"" + filePath + "\"");
                            }
                        }
                    }
                }

                return true;
            }

            public bool SetAnalyzers(ITaskItem[] analyzerReferences)
            {
                if (analyzerReferences != null)
                {
                    foreach (var ar in analyzerReferences)
                    {
                        var filePath = GetDocumentFilePath(ar);
                        CommandLineArgs.Add("/analyzer:\"" + filePath + "\"");
                    }
                }

                return true;
            }

            public bool SetAdditionalFiles(ITaskItem[] additionalFiles)
            {
                if (additionalFiles != null && additionalFiles.Length > 0)
                {
                    AdditionalSources = additionalFiles.Select(GetDocumentFilePath).ToArray();
                }

                return true;
            }

            public bool SetResources(ITaskItem[] resources)
            {
                if (resources != null && resources.Length > 0)
                {
                    foreach (var r in resources)
                    {
                        CommandLineArgs.Add("/resource:\"" + GetDocumentFilePath(r) + "\"");
                    }
                }

                return true;
            }

            public bool SetResponseFiles(ITaskItem[] responseFiles)
            {
                if (responseFiles != null && responseFiles.Length > 0)
                {
                    foreach (var rf in responseFiles)
                    {
                        CommandLineArgs.Add("@\"" + GetDocumentFilePath(rf) + "\"");
                    }
                }

                return true;
            }

            public bool SetSources(ITaskItem[] sources)
            {
                if (sources != null && sources.Length > 0)
                {
                    Sources = sources.Select(GetDocumentFilePath).ToArray();
                }

                return true;
            }

            public bool SetTargetType(string targetType)
            {
                if (!string.IsNullOrWhiteSpace(targetType))
                {
                    _targetType = targetType.ToLower();
                    CommandLineArgs.Add("/target:" + _targetType);
                }

                return true;
            }

            public bool SetRuleSet(string ruleSetFile)
            {
                if (!string.IsNullOrWhiteSpace(ruleSetFile))
                {
                    CommandLineArgs.Add("/ruleset:\"" + ruleSetFile + "\"");
                }

                return true;
            }

            public bool SetTreatWarningsAsErrors(bool treatWarningsAsErrors)
            {
                if (treatWarningsAsErrors)
                {
                    CommandLineArgs.Add("/warnaserror");
                }

                return true;
            }

            public bool SetWarningLevel(int warningLevel)
            {
                CommandLineArgs.Add("/warn:" + warningLevel);
                return true;
            }

            public bool SetWarningsAsErrors(string warningsAsErrors)
            {
                if (!string.IsNullOrWhiteSpace(warningsAsErrors))
                {
                    CommandLineArgs.Add("/warnaserror+:" + warningsAsErrors);
                }

                return true;
            }

            public bool SetWarningsNotAsErrors(string warningsNotAsErrors)
            {
                if (!string.IsNullOrWhiteSpace(warningsNotAsErrors))
                {
                    CommandLineArgs.Add("/warnaserror-:" + warningsNotAsErrors);
                }

                return true;
            }

            public bool SetWin32Icon(string win32Icon)
            {
                if (!string.IsNullOrWhiteSpace(win32Icon))
                {
                    CommandLineArgs.Add("/win32icon:\"" + win32Icon + "\"");
                }

                return true;
            }

            public bool SetWin32Resource(string win32Resource)
            {
                if (!string.IsNullOrWhiteSpace(win32Resource))
                {
                    CommandLineArgs.Add("/win32res:\"" + win32Resource + "\"");
                }

                return true;
            }

            protected string GetAbsolutePath(string path)
            {
                return Path.GetFullPath(Path.Combine(_baseDirectory, path));
            }

            protected string GetDocumentFilePath(ITaskItem documentItem)
            {
                return GetAbsolutePath(documentItem.ItemSpec);
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


    internal static class SemaphoreSlimExtensions
    {
        public static SemaphoreDisposer DisposableWait(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default(CancellationToken))
        {
            semaphore.Wait(cancellationToken);
            return new SemaphoreDisposer(semaphore);
        }

        public async static Task<SemaphoreDisposer> DisposableWaitAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default(CancellationToken))
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new SemaphoreDisposer(semaphore);
        }

        internal struct SemaphoreDisposer : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public SemaphoreDisposer(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}

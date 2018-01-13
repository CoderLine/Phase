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
            var result = await BuildAsync(buildParameters, buildRequestData, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (result.OverallResult == BuildResultCode.Failure)
            {
                throw result.Exception ?? new InvalidOperationException("Error during project compilation");
            }

            return await compilationHost.CompileAsync(cancellationToken);
        }

        private static readonly SemaphoreSlim BuildManagerLock = new SemaphoreSlim(initialCount: 1);
        private async Task<BuildResult> BuildAsync(BuildParameters parameters, BuildRequestData requestData, CancellationToken cancellationToken)
        {
            // only allow one build to use the default build manager at a time
            using (await BuildManagerLock.DisposableWaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
            {
                return await BuildAsync(BuildManager.DefaultBuildManager, parameters, requestData, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private static Task<BuildResult> BuildAsync(BuildManager buildManager, BuildParameters parameters, BuildRequestData requestData, CancellationToken cancellationToken)
        {
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

            public void Initialize(IEventSource eventSource)
            {
                _eventSource = eventSource;
                _eventSource.BuildStarted += OnBuildStarted;
                _eventSource.ErrorRaised += OnErrorRaised;
                _eventSource.TargetStarted += OnTargetStarted;
                _eventSource.TargetFinished += OnTargetFinished;
                _eventSource.BuildFinished += OnBuildFinished;
                _eventSource.ProjectStarted += OnProjectStarted;
                _eventSource.ProjectFinished += OnProjectFinished;
            }

            private void OnProjectFinished(object sender, ProjectFinishedEventArgs e)
            {
                Log.Trace($"Preparing project {Path.GetFileName(e.ProjectFile)} finished");
            }

            private void OnProjectStarted(object sender, ProjectStartedEventArgs e)
            {
                Log.Trace($"Preparing project {Path.GetFileName(e.ProjectFile)} started");
            }

            private void OnBuildFinished(object sender, BuildFinishedEventArgs e)
            {
                Log.Trace($"C# compile preparation finished");
            }

            private void OnBuildStarted(object sender, BuildStartedEventArgs e)
            {
                Log.Trace($"C# compile preparation started");
            }

            private void OnTargetFinished(object sender, TargetFinishedEventArgs e)
            {
                //Log.Trace($"'{e.TargetName}' Finished");
            }

            private void OnTargetStarted(object sender, TargetStartedEventArgs e)
            {
                //Log.Trace($"'{e.TargetName}' Started");
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

            Task<Compilation> CompileAsync(CancellationToken cancellationToken);
        }

        public class CSharpCompilationHost : ICompilationHost, ICscHostObject4
        {
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

            public async Task<Compilation> CompileAsync(CancellationToken cancellationToken)
            {
                var args = CSharpCommandLineParser.Default.Parse(CommandLineArgs, _baseDirectory,
                    RuntimeEnvironment.GetRuntimeDirectory());

                var trees = new SyntaxTree[Sources.Length];
                var parseErrors = new ConcurrentBag<Exception>();
                Parallel.For(0, Sources.Length, i =>
                {
                    try
                    {
                        using (var s = new FileStream(Sources[i], FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
                            FileOptions.Asynchronous))
                        {
                            trees[i] = CSharpSyntaxTree.ParseText(SourceText.From(s),
                                args.ParseOptions,
                                Sources[i]);
                        }
                    }
                    catch (Exception e)
                    {
                        parseErrors.Add(e);
                    }
                });
                if (parseErrors.Count > 0)
                {
                    throw new AggregateException(parseErrors);
                }

                var resolver = new WorkspaceMetadataFileReferenceResolver(_baseDirectory);

                return CSharpCompilation.Create(
                    args.CompilationOptions.ModuleName,
                    trees,
                    args.ResolveMetadataReferences(resolver),
                    options: args.CompilationOptions
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

        private sealed class WorkspaceMetadataFileReferenceResolver : MetadataReferenceResolver, IEquatable<WorkspaceMetadataFileReferenceResolver>
        {
            private readonly string _baseDir;
            private readonly MetadataReferenceCache _cache;

            public WorkspaceMetadataFileReferenceResolver(string baseDir)
            {
                _baseDir = baseDir;
                _cache = new MetadataReferenceCache((path, properties) =>
                    MetadataReference.CreateFromFile(path, properties));
            }

            public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
            {
                var path = Path.Combine(baseFilePath ?? _baseDir, reference);
                return ImmutableArray.Create((PortableExecutableReference)_cache.GetReference(path, properties));
            }

            public bool Equals(WorkspaceMetadataFileReferenceResolver other)
            {
                return other != null && _baseDir.Equals(other._baseDir);
            }

            public override int GetHashCode()
            {
                return _baseDir.GetHashCode();
            }

            public override bool Equals(object other) => Equals(other as WorkspaceMetadataFileReferenceResolver);
        }

        private class MetadataReferenceCache
        {
            private ImmutableDictionary<string, ReferenceSet> _referenceSets = ImmutableDictionary<string, ReferenceSet>.Empty;

            private readonly Func<string, MetadataReferenceProperties, MetadataReference> _createReference;

            public MetadataReferenceCache(Func<string, MetadataReferenceProperties, MetadataReference> createReference)
            {
                _createReference = createReference;
            }

            public MetadataReference GetReference(string path, MetadataReferenceProperties properties)
            {
                if (!_referenceSets.TryGetValue(path, out var referenceSet))
                {
                    referenceSet = ImmutableInterlocked.GetOrAdd(ref _referenceSets, path, new ReferenceSet(this));
                }

                return referenceSet.GetAddOrUpdate(path, properties);
            }

            private class ReferenceSet
            {
                private readonly MetadataReferenceCache _cache;

                private readonly NonReentrantLock _gate = new NonReentrantLock();

                // metadata references are held weakly, so even though this is a cache that enables reuse, it does not control lifetime.
                private readonly Dictionary<MetadataReferenceProperties, WeakReference<MetadataReference>> _references
                    = new Dictionary<MetadataReferenceProperties, WeakReference<MetadataReference>>();

                public ReferenceSet(MetadataReferenceCache cache)
                {
                    _cache = cache;
                }

                public MetadataReference GetAddOrUpdate(string path, MetadataReferenceProperties properties)
                {
                    using (_gate.DisposableWait())
                    {
                        WeakReference<MetadataReference> weakref;
                        MetadataReference mref = null;

                        if (!(_references.TryGetValue(properties, out weakref) && weakref.TryGetTarget(out mref)))
                        {
                            // try to base this metadata reference off of an existing one, so we don't load the metadata bytes twice.
                            foreach (var wr in _references.Values)
                            {
                                if (wr.TryGetTarget(out mref))
                                {
                                    mref = mref.WithProperties(properties);
                                    break;
                                }
                            }

                            if (mref == null)
                            {
                                mref = _cache._createReference(path, properties);
                            }

                            _references[properties] = new WeakReference<MetadataReference>(mref);
                        }

                        return mref;
                    }
                }
            }
        }


        internal sealed class NonReentrantLock
        {
            private readonly object _syncLock;
            private volatile int _owningThreadId;
            public NonReentrantLock(bool useThisInstanceForSynchronization = false)
            {
                _syncLock = useThisInstanceForSynchronization ? this : new object();
            }

            public void Wait(CancellationToken cancellationToken = default(CancellationToken))
            {
                if (this.IsOwnedByMe)
                {
                    throw new LockRecursionException();
                }

                CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);

                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Fast path to try and avoid allocations in callback registration.
                    lock (_syncLock)
                    {
                        if (!this.IsLocked)
                        {
                            this.TakeOwnership();
                            return;
                        }
                    }

                    cancellationTokenRegistration = cancellationToken.Register(CancellationTokenCanceledEventHandler, _syncLock, useSynchronizationContext: false);
                }

                using (cancellationTokenRegistration)
                {
                    // PERF: First spin wait for the lock to become available, but only up to the first planned yield.
                    // This additional amount of spinwaiting was inherited from SemaphoreSlim's implementation where
                    // it showed measurable perf gains in test scenarios.
                    SpinWait spin = new SpinWait();
                    while (this.IsLocked && !spin.NextSpinWillYield)
                    {
                        spin.SpinOnce();
                    }

                    lock (_syncLock)
                    {
                        while (this.IsLocked)
                        {
                            // If cancelled, we throw. Trying to wait could lead to deadlock.
                            cancellationToken.ThrowIfCancellationRequested();

                            {
                                // Another thread holds the lock. Wait until we get awoken either
                                // by some code calling "Release" or by cancellation.
                                Monitor.Wait(_syncLock);
                            }
                        }

                        // We now hold the lock
                        this.TakeOwnership();
                    }
                }
            }

            /// <summary>
            /// Exit the mutual exclusion.
            /// </summary>
            /// <remarks>
            /// The calling thread must currently hold the lock.
            /// </remarks>
            /// <exception cref="InvalidOperationException">The lock is not currently held by the calling thread.</exception>
            public void Release()
            {
                lock (_syncLock)
                {
                    this.ReleaseOwnership();

                    // Release one waiter
                    Monitor.Pulse(_syncLock);
                }
            }

            private bool IsLocked => _owningThreadId != 0;
            private bool IsOwnedByMe => _owningThreadId == Environment.CurrentManagedThreadId;
            private void TakeOwnership()
            {
                _owningThreadId = Environment.CurrentManagedThreadId;
            }
            private void ReleaseOwnership()
            {
                _owningThreadId = 0;
            }

            private static void CancellationTokenCanceledEventHandler(object obj)
            {
                lock (obj)
                {
                    // Release all waiters to check their cancellation tokens.
                    Monitor.PulseAll(obj);
                }
            }

            public SemaphoreDisposer DisposableWait(CancellationToken cancellationToken = default(CancellationToken))
            {
                this.Wait(cancellationToken);
                return new SemaphoreDisposer(this);
            }

            public struct SemaphoreDisposer : IDisposable
            {
                private readonly NonReentrantLock _semaphore;

                public SemaphoreDisposer(NonReentrantLock semaphore)
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

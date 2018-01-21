using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Phase.Translator.Utils;

namespace Phase.Translator
{
    public class PhaseCompiler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public PhaseCompilerInput Input { get; private set; }
        public PhaseCompilerOptions Options { get; private set; }
        public PhaseTranslator Translator { get; private set; }

        public PhaseCompiler(PhaseCompilerInput compilerInput)
        {
            Input = compilerInput;
        }

        public async Task CompileAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                using (new LogHelper("compilation", Log))
                {
                    await ReadOptionsAsync();
                    await TranslateAsync(cancellationToken);
                    WriteOutput();
                    ExecutePostBuild(cancellationToken);
                }
            }
            catch (PhaseCompilerException)
            {
                throw;
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected error during compilation");
                throw;
            }
        }

        private void ExecutePostBuild(CancellationToken cancellationToken)
        {
            if (Options.PostBuild == null) return;
            foreach (var step in Options.PostBuild)
            {
                ExecutePostBuildStep(step, cancellationToken);
            }
        }

        private void ExecutePostBuildStep(PostBuildStep step, CancellationToken cancellationToken)
        {
            using (new LogHelper($"post build step '{step.Name}'", Log))
            {
                try
                {
                    var cmd = new Process
                    {
                        StartInfo =
                        {
                            FileName = step.Executable,
                            Arguments = step.Arguments,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            WorkingDirectory = Path.GetDirectoryName(Input.ProjectFile)
                        }
                    };

                    cmd.ErrorDataReceived += (sender, args) => { Log.Error(args.Data); };
                    cmd.OutputDataReceived += (sender, args) => { Log.Trace(args.Data); };

                    using (cancellationToken.Register(() => { cmd.Kill(); }))
                    {
                        cmd.Start();

                        cmd.BeginOutputReadLine();
                        cmd.BeginErrorReadLine();

                        cmd.WaitForExit();

                        if (cmd.ExitCode != 0)
                        {
                            Log.Trace($"Process exited with code {cmd.ExitCode}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error on postbuild execution");
                }
            }
        }

        private async Task ReadOptionsAsync()
        {
            Log.Trace("Start reading options");

            var phaseConfig = Path.Combine(Path.GetDirectoryName(Input.ProjectFile), PhaseCompilerOptions.ConfigFileName);
            if (!File.Exists(phaseConfig))
            {
                var msg = $"Could not find {PhaseCompilerOptions.ConfigFileName} beside project file";
                Log.Error(msg);
                throw new PhaseCompilerException(msg);
            }

            Options = await PhaseCompilerOptions.FromFileAsync(phaseConfig);

            Log.Trace("Finished reading options");
        }

        private async Task TranslateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Translator = new PhaseTranslator(this);
            await Translator.TranslateAsync(cancellationToken);
        }

        private void WriteOutput()
        {
            using (new LogHelper("writing output", Log, 1))
            {
                if (Translator.Result != null)
                {
                    Log.Trace($"writing ({Translator.Result.Results.Count} files) to {Options.Output}");

                    if (!Directory.Exists(Options.Output))
                    {
                        Directory.CreateDirectory(Options.Output);
                    }

                    Parallel.ForEach(Translator.Result.Results, results =>
                    {
                        var fileName = Path.Combine(Options.Output, results.Value.FileName);
                        var dir = Path.GetDirectoryName(fileName);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        File.WriteAllText(fileName, results.Value.SourceCode);
                    });
                }
            }
        }

    }
}

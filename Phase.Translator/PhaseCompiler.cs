using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        public const string Preprocessor = "PHASE";

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
                    var workingDirectory = Path.GetDirectoryName(Input.ProjectFile);
                    if (!string.IsNullOrEmpty(step.WorkingDirectory))
                    {
                        workingDirectory = Path.IsPathRooted(step.WorkingDirectory)
                            ? step.WorkingDirectory
                            : Path.Combine(workingDirectory, step.WorkingDirectory);
                    }
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
                            WorkingDirectory = workingDirectory
                        }
                    };

                    bool translateHaxeOutput =
                        step.Executable.Equals("haxe.exe", StringComparison.InvariantCultureIgnoreCase) ||
                        step.Executable.Equals("haxe", StringComparison.InvariantCultureIgnoreCase);

                    cmd.ErrorDataReceived += (sender, args) => { Log.Error(translateHaxeOutput ? TranslateHaxeOutput(args.Data) : args.Data); };
                    cmd.OutputDataReceived += (sender, args) => { Log.Trace(translateHaxeOutput ? TranslateHaxeOutput(args.Data) : args.Data); };

                    using (cancellationToken.Register(() => { cmd.Kill(); }))
                    {
                        cmd.Start();

                        cmd.BeginOutputReadLine();
                        cmd.BeginErrorReadLine();

                        cmd.WaitForExit();

                        if (cmd.ExitCode != 0)
                        {
                            throw new PhaseCompilerException($"Post build step failed with code {cmd.ExitCode}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error on postbuild execution");
                    throw;
                }
            }
        }

        private static readonly Regex HaxeError = new Regex(@"(?<File>.*\.hx):(?<Line>[0-9]+): characters (?<CharStart>[0-9]+)-(?<CharEnd>[0-9]+) : (?<Message>.*)", RegexOptions.Compiled);
        private string TranslateHaxeOutput(string text)
        {
            if (text == null) return string.Empty;
            return HaxeError.Replace(text, m =>
            {
                var errorWarning =
                    CultureInfo.InvariantCulture.CompareInfo.IndexOf(m.Groups["Message"].Value, "warning",
                        CompareOptions.IgnoreCase) >= 0
                        ? "warning PH000"
                        : "error PH000";
                var file = m.Groups["File"].Value
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar)
                    ;
                file = Path.Combine(Path.GetDirectoryName(Input.ProjectFile), file);
                return $"{file}({m.Groups["Line"].Value},{m.Groups["CharStart"].Value},{m.Groups["Line"].Value},{m.Groups["CharEnd"]}): {errorWarning} : {m.Groups["Message"].Value}";
            });
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
                    Log.Trace($"writing ({Translator.Result.Results.Sum(r=>r.Value.Count)} files) to {Options.Output}");

                    if (!Directory.Exists(Options.Output))
                    {
                        Directory.CreateDirectory(Options.Output);
                    }

                    Parallel.ForEach(Translator.Result.Results.SelectMany(r=>r.Value), results =>
                    {
                        var fileName = Path.Combine(Options.Output, results.FileName);
                        var dir = Path.GetDirectoryName(fileName);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        File.WriteAllText(fileName, results.SourceCode);
                    });
                }
            }
        }

    }
}

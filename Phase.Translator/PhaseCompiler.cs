using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;

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

        public async Task Compile(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                Log.Trace("Start compilation");

                await ReadOptionsAsync();
                await TranslateAsync(cancellationToken);
                await WriteOutputAsync();

                Log.Trace("Finished compilation");

            }
            catch (PhaseCompilerException)
            {
                throw;
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected error during compilation");
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
            Log.Trace("Start translation");

            Translator = new PhaseTranslator(this);
            await Translator.TranslateAsync(cancellationToken);

            Log.Trace("Finished translation");
        }

        private async Task WriteOutputAsync()
        {
            if (Translator.Result != null)
            {
                Log.Trace($"Start writing output ({Translator.Result.Results.Count} files) to {Options.Output}");

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
             
                Log.Trace("Finished writing output");
            }
        }

    }
}

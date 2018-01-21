using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Phase.Translator
{
    public class PhaseCompilerOptions
    {
        public const string ConfigFileName = "phase.json";

        public string Output { get; set; }
        public PhaseLanguage Language { get; set; }

        public PostBuildStep[] PostBuild { get; set; }

        public PhaseCompilerOptions()
        {
            Language = PhaseLanguage.Haxe;
        }

        public static async Task<PhaseCompilerOptions> FromFileAsync(string fileName)
        {
            using (var fs = new StreamReader(File.OpenRead(fileName)))
            {
                string content = await fs.ReadToEndAsync();
                var config = JsonConvert.DeserializeObject<PhaseCompilerOptions>(content);

                var fileDirectory = new FileInfo(fileName).Directory.FullName;
                var currentWorkingDirectory = Environment.CurrentDirectory;
                Environment.CurrentDirectory = fileDirectory;

                var dir = new DirectoryInfo(config.Output);
                config.Output = dir.FullName;

                Environment.CurrentDirectory = currentWorkingDirectory;

                return config;
            }
        }
    }


    public class PostBuildStep
    {
        public string Name { get; set; }
        public string Executable { get; set; }
        public string Arguments { get; set; }
    }
}
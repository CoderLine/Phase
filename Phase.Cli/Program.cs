using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Phase.Translator;

namespace Phase.Cli
{
    class Program
    {
        static IEnumerable<int> Test()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return i;

                if (i == 7)
                {
                    yield break;
                }
            }
        }

        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            if (args.Length != 3)
            {
                Console.WriteLine("Phase.Cli.exe ProjectFile Configuration Platform");
                return;
            }

            var input = new PhaseCompilerInput
            {
                ProjectFile = args[0],
                Configuration = args[1],
                Platform = args[2]
            };

            try
            {
                var compiler = new PhaseCompiler(input);
                compiler.Compile().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}

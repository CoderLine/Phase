using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Phase.Translator;

namespace Phase.Cli
{
    class Program
    {
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
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            }
        }
    }
}

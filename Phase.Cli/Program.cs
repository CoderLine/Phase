using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

            string testSrc =
@"
using System;

class Test { 
public function Main() {
#if POSITIVE
    int i = 1;
#endif 

#if NEGATIVE
    int x = 2;
#endif 

#if !POSITIVE
    int j = 2;
#endif 
}

} 
";

            var tree = CSharpSyntaxTree.ParseText(testSrc,
                CSharpParseOptions.Default.WithPreprocessorSymbols("POSITIVE"));

            var input = new PhaseCompilerInput
            {
                ProjectFile = args[0],
                Configuration = args[1],
                Platform = args[2]
            };

            try
            {
                var compiler = new PhaseCompiler(input);
                compiler.CompileAsync().Wait();
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

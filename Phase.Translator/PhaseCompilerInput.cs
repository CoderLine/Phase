using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Phase.Translator
{
    public class PhaseCompilerInput
    {
        public string ProjectFile { get; set; }

        public string Platform { get; set; }
        public string Configuration { get; set; }

        public string[] SourceFiles { get; set; }
        public IEnumerable<MetadataReference> ReferencedAssemblies { get; set; }

        public CSharpCompilationOptions CompilationOptions { get; set; }
        public CSharpParseOptions ParseOptions { get; set; }

    }
}

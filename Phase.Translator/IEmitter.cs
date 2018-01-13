
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Phase.Translator.Haxe;

namespace Phase.Translator
{
    public class EmitResult
    {
        public Dictionary<PhaseType, PhaseTypeResult> Results { get; }

        public EmitResult()
        {
            Results = new Dictionary<PhaseType, PhaseTypeResult>();
        }
    }

    public class PhaseTypeResult
    {
        public string FileName { get; }
        public string SourceCode { get; }

        public PhaseTypeResult(string fileName, string sourceCode)
        {
            FileName = fileName;
            SourceCode = sourceCode;
        }
    }

    public interface IEmitter
    {
        PhaseCompiler Compiler { get; }
        Task<EmitResult> EmitAsync(CSharpCompilation compilation, IEnumerable<PhaseType> types, CancellationToken cancellationToken);
    }
}

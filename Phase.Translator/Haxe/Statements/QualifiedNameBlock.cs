using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class QualifiedNameBlock : CommentedNodeEmitBlock<QualifiedNameSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var type = Emitter.GetTypeInfo(Node);
            if (type.Type != null)
            {
                Write(Emitter.GetTypeName(type.Type));
            }
            else
            {
                throw new PhaseCompilerException("Could not resolve qualified name to type");
            }
        }
    }
}
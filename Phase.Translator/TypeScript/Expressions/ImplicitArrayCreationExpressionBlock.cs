using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript.Expressions
{
    public class ImplicitArrayCreationExpressionBlock : AbstractTypeScriptEmitterBlock<ImplicitArrayCreationExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var elementType = ((IArrayTypeSymbol)Emitter.GetTypeInfo(Node).Type).ElementType;
            var specialArray = Emitter.GetSpecialArrayName(elementType);

            if (specialArray != null)
            {
                Write("new ", specialArray, "(");
            }

            
            WriteOpenBracket();

            for (int i = 0; i < Node.Initializer.Expressions.Count; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                }
                EmitTree(Node.Initializer.Expressions[i], cancellationToken);
            }

            WriteCloseBracket();
            
            if (specialArray != null)
            {
                Write(")");
            }
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript.Expressions
{
    public class InitializerExpressionBlock : AbstractTypeScriptEmitterBlock<InitializerExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var specialArray = TryGetSpecialArray();
            if (specialArray != null)
            {
                Write("new ", specialArray, "(");
            }

            WriteOpenBracket();
            EmitterContext.InitializerCount++;

            for (int i = 0; i < Node.Expressions.Count; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                }

                EmitTree(Node.Expressions[i], cancellationToken);
            }

            EmitterContext.InitializerCount--;
            WriteCloseBracket();

            if (specialArray != null)
            {
                WriteCloseParentheses();
            }
        }

        private string TryGetSpecialArray()
        {
            var typeInfo = Emitter.GetTypeInfo(Node);
            if (typeInfo.Type == null && typeInfo.ConvertedType != null &&
                typeInfo.ConvertedType is IArrayTypeSymbol arrayType)
            {
                var elementType = arrayType.ElementType;
                return Emitter.GetSpecialArrayName(elementType);
            }

            if (Node.Parent.Kind() == SyntaxKind.ArrayCreationExpression)
            {
                typeInfo = Emitter.GetTypeInfo(Node.Parent);
                if (typeInfo.Type is IArrayTypeSymbol parentArrayType)
                {
                    var elementType = parentArrayType.ElementType;
                    return Emitter.GetSpecialArrayName(elementType);
                }
            }

            return null;
        }
    }
}
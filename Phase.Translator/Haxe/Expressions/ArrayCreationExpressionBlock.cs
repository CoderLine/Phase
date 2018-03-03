using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class ArrayCreationExpressionBlock : AbstractHaxeScriptEmitterBlock<ArrayCreationExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Node.Initializer != null)
            {
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
            }
            else
            {
                var elementType = Emitter.GetTypeSymbol(Node.Type.ElementType);
                var specialArray = Emitter.GetSpecialArrayName(elementType);

                if (specialArray != null && Node.Type.RankSpecifiers.Count == 1)
                {
                    Write(specialArray);
                    WriteDot();
                    Write("empty");
                }
                else
                {
                    Write("system.FixedArray");
                    WriteDot();
                    Write("empty");
                }

                if (Node.Type.RankSpecifiers.Count > 1)
                {
                    Write(Node.Type.RankSpecifiers.Count);
                }

                WriteOpenParentheses();

                var x = 0;
                for (int i = 0; i < Node.Type.RankSpecifiers.Count; i++)
                {
                   
                    for (int j = 0; j < Node.Type.RankSpecifiers[i].Sizes.Count; j++)
                    {
                        var expr = Node.Type.RankSpecifiers[i].Sizes[j];
                        
                        if (expr.Kind() != SyntaxKind.OmittedArraySizeExpression)
                        {
                            if (x > 0)
                            {
                                WriteComma();
                            }
                            EmitTree(Node.Type.RankSpecifiers[i].Sizes[j], cancellationToken);
                            x++;
                        }
                    }
                }
                WriteCloseParentheses();
            }

        }
    }
}
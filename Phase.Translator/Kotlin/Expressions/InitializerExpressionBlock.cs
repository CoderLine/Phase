using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    public class InitializerExpressionBlock : AbstractKotlinEmitterBlock<InitializerExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var typeInfo = Emitter.GetTypeInfo(Node, cancellationToken);
            var type = typeInfo.Type ?? typeInfo.ConvertedType;
            string arrayFunction = null;
            if (type is IArrayTypeSymbol array)
            {
                arrayFunction = Emitter.GetArrayCreationFunctionName(array);
            }
            else if (Node.Kind() == SyntaxKind.ArrayInitializerExpression && Node.Expressions.Count > 0)
            {
                typeInfo = Emitter.GetTypeInfo(Node.Expressions[0]);
                type = typeInfo.Type ?? typeInfo.ConvertedType;
                if (type != null)
                {
                    arrayFunction = Emitter.GetArrayCreationFunctionName(type);
                }
            }

            if (arrayFunction != null)
            {
                Write(arrayFunction);
                WriteOpenParentheses();

                for (var i = 0; i < Node.Expressions.Count; i++)
                {
                    var expression = Node.Expressions[i];
                    if (i > 0) WriteComma();
                    EmitTree(expression, cancellationToken);
                }

                WriteCloseParentheses();
            }
            else
            {
                BeginBlock();

                for (var i = 0; i < Node.Expressions.Count; i++)
                {
                    var expression = Node.Expressions[i];
                    if (i > 0) WriteComma();
                    EmitTree(expression, cancellationToken);
                }

                EndBlock();
            }
        }
    }
}
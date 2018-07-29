using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Expressions
{
    class ParenthesizedLambdaExpressionBlock : AutoCastBlockBase<ParenthesizedLambdaExpressionSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteOpenParentheses();

            var type = Emitter.GetTypeInfo(Node).Type;
            var method = (type as INamedTypeSymbol)?.DelegateInvokeMethod;

            for (int i = 0; i < Node.ParameterList.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                }

                var parameter = Emitter.GetDeclaredSymbol(Node.ParameterList.Parameters[i], cancellationToken);

                if (Node.ParameterList.Parameters[i].Type != null)
                {
                    var needsBoxedType = method != null && method.Parameters[i].OriginalDefinition.Type.TypeKind == TypeKind.TypeParameter;
                    Write(Emitter.GetTypeName(parameter.Type, false, false, needsBoxedType));
                    WriteSpace();
                }
                Write(Node.ParameterList.Parameters[i].Identifier.Text);
            }

            WriteCloseParentheses();
            Write(" -> ");
            EmitTree(Node.Body, cancellationToken);
            return AutoCastMode.SkipCast;
        }
    }
}
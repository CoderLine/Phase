using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    class ParenthesizedLambdaExpressionBlock : AbstractCppEmitterBlock<ParenthesizedLambdaExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            if (EmitterContext.CurrentMember.IsStatic)
            {
                Write("[&]");
            }
            else
            {
                Write("[&,this]");
            }

            WriteOpenParentheses();

            for (int i = 0; i < Node.ParameterList.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                }

                var parameter = Emitter.GetDeclaredSymbol(Node.ParameterList.Parameters[i], cancellationToken);
                WriteType(parameter.Type);
                WriteSpace();
                Write(Node.ParameterList.Parameters[i].Identifier.Text);
            }


            WriteCloseParentheses();

            if (Node.Body.Kind() == SyntaxKind.Block)
            {
                EmitTree(Node.Body, cancellationToken);
            }
            else
            {
                BeginBlock();

                var symbol = Emitter.GetSymbolInfo(Node, cancellationToken);
                if (symbol.Symbol.Kind == SymbolKind.Method && !((IMethodSymbol)symbol.Symbol).ReturnsVoid)
                {
                    Write("return ");
                }
                EmitTree(Node.Body, cancellationToken);

                WriteSemiColon(true);
                EndBlock();
            }
        }
    }
}
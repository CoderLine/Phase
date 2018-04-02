using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    class SimpleLambdaExpressionBlock : AbstractCppEmitterBlock<SimpleLambdaExpressionSyntax>
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

            var parameter = Emitter.GetDeclaredSymbol(Node.Parameter, cancellationToken);
            WriteType(parameter.Type);
            WriteSpace();
            Write(Node.Parameter.Identifier.Text);

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
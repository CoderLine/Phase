using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    class SimpleLambdaExpressionBlock : AbstractHaxeScriptEmitterBlock<SimpleLambdaExpressionSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteFunction();
            WriteOpenParentheses();

            Write(Node.Parameter.Identifier.Text);

            if (Node.Parameter.Type != null)
            {
                WriteColon();
                WriteType(Node.Parameter.Type);
            }

            WriteCloseParentheses();


            if (Node.Body.Kind() == SyntaxKind.Block)
            {
                await EmitTreeAsync(Node.Body, cancellationToken);
            }
            else
            {
                BeginBlock();

                var symbol = Emitter.GetSymbolInfo(Node, cancellationToken);
                if (symbol.Symbol.Kind == SymbolKind.Method && !((IMethodSymbol)symbol.Symbol).ReturnsVoid)
                {
                    Write("return ");
                }
                await EmitTreeAsync(Node.Body, cancellationToken);

                WriteSemiColon(true);
                EndBlock();
            }
        }
    }
}
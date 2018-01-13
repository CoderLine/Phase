using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    class ParenthesizedLambdaExpressionBlock : AbstractHaxeScriptEmitterBlock<ParenthesizedLambdaExpressionSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteFunction();
            WriteOpenParentheses();

            for (int i = 0; i < Node.ParameterList.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                }
                Write(Node.ParameterList.Parameters[i].Identifier.Text);

                if (Node.ParameterList.Parameters[i].Type != null)
                {
                    WriteColon();
                    WriteType(Node.ParameterList.Parameters[i].Type);
                }
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
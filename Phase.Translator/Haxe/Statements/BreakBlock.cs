using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class BreakBlock : AbstractHaxeScriptEmitterBlock<BreakStatementSyntax>
    {
        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Node.Parent.Kind() != SyntaxKind.SwitchSection)
            {
                Write("break");
                WriteSemiColon(true);
            }
        }
    }
}
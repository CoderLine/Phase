using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Statements
{
    public class ContinueBlock : CommentedNodeEmitBlock<ContinueStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var hasSwitch = false;
            var loop = Node.Parent;
            while (loop != null && loop.Kind() != SyntaxKind.ForEachStatement && loop.Kind() != SyntaxKind.WhileStatement && loop.Kind() != SyntaxKind.DoStatement && loop.Kind() != SyntaxKind.ForStatement)
            {
                if (loop.Kind() == SyntaxKind.SwitchStatement)
                {
                    hasSwitch = true;
                }
                loop = loop.Parent;
            }

            if (!hasSwitch) loop = null;

            if (loop != null)
            {
                if (!EmitterContext.LoopNames.TryGetValue(loop, out var loopName))
                {
                    EmitterContext.LoopNames[loop] = loopName = "_" + Guid.NewGuid().ToString("N");
                }
                Write("continue@", loopName);
                WriteSemiColon(true);
            }
            else
            {
                Write("continue");
                WriteSemiColon(true);
            }
        }
    }
}
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Statements
{
    public class ForBlock : CommentedNodeEmitBlock<ForStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            Write("run ");
            BeginBlock();

            if (Node.Declaration != null)
            {
                EmitTree(Node.Declaration, cancellationToken);
            }
            else if (Node.Initializers.Count > 0)
            {
                foreach (var initializer in Node.Initializers)
                {
                    EmitTree(initializer, cancellationToken);
                    WriteSemiColon(true);
                }
            }


            PushWriter();
            BeginBlock();

            EmitterContext.CurrentForIncrementors.Push(Node.Incrementors);

            if (Node.Statement.Kind() == SyntaxKind.Block)
            {
                foreach (var statement in ((BlockSyntax)Node.Statement).Statements)
                {
                    EmitTree(statement, cancellationToken);
                }
            }
            else
            {
                // TODO: continue statements can spoil incrementors!
                EmitTree(Node.Statement, cancellationToken);
            }

            foreach (var incrementor in Node.Incrementors)
            {
                EmitTree(incrementor, cancellationToken);
                WriteSemiColon(true);
            }

            EmitterContext.CurrentForIncrementors.Pop();

            EndBlock();
            var body = PopWriter();


            if (EmitterContext.LoopNames.TryGetValue(Node, out var name))
            {
                Write(name, "@ ");
                EmitterContext.LoopNames.Remove(Node);
            }

            WriteWhile();
            WriteOpenParentheses();
            EmitTree(Node.Condition, cancellationToken);
            WriteCloseParentheses();

            WriteNewLine();

            Write(body);

            EndBlock();
        }
    }
}
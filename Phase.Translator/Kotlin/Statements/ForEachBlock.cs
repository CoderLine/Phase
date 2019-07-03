using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Attributes;

namespace Phase.Translator.Kotlin.Statements
{
    public class ForEachBlock : CommentedNodeEmitBlock<ForEachStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            EmitterContext.CurrentForIncrementors.Push(null);

            var type = Emitter.GetTypeInfo(Node.Expression);
            var foreachMode = Emitter.GetForeachMode(type.Type) ?? ForeachMode.AsIterable;

            if (foreachMode == ForeachMode.Native || foreachMode == ForeachMode.AsIterable)
            {
                PushWriter();
                EmitTree(Node.Statement, cancellationToken);
                var body = PopWriter();

                if (EmitterContext.LoopNames.TryGetValue(Node, out var name))
                {
                    Write(name, "@ ");
                    EmitterContext.LoopNames.Remove(Node);
                }
                WriteFor();

                WriteOpenParentheses();

                Write(Node.Identifier.ValueText);

                Write(" in ");

                EmitTree(Node.Expression, cancellationToken);
                if (foreachMode == ForeachMode.AsIterable)
                {
                    Write(".wrapAsIterable()");
                }

                WriteCloseParentheses();

                if (Node.Statement is BlockSyntax)
                {
                    WriteNewLine();
                }

                Write(body.TrimStart());

                WriteNewLine();

            }
            else
            {
                Write("run ");
                BeginBlock();

                var enumeratorVariable = "__e";
                if (EmitterContext.RecursiveForeach > 0)
                {
                    enumeratorVariable += EmitterContext.RecursiveForeach;
                }
                EmitterContext.RecursiveForeach++;

                Write("var ", enumeratorVariable, " = ");
                Write("(");
                EmitTree(Node.Expression, cancellationToken);
                Write(").getEnumerator()");
                WriteSemiColon(true);

                Write("try");
                BeginBlock();

                PushWriter();
                BeginBlock();

                Write("var ", Node.Identifier.ValueText, " = ", enumeratorVariable, ".current");
                WriteSemiColon(true);

                if (Node.Statement is BlockSyntax block)
                {
                    foreach (var statement in block.Statements)
                    {
                        EmitTree(statement, cancellationToken);
                    }
                }
                else
                {
                    EmitTree(Node.Statement, cancellationToken);
                }

                EndBlock();
                var body = PopWriter();

                if (EmitterContext.LoopNames.TryGetValue(Node, out var name))
                {
                    Write(name, "@ ");
                    EmitterContext.LoopNames.Remove(Node);
                }

                WriteWhile();
                WriteOpenParentheses();
                Write(enumeratorVariable, ".moveNext()");
                WriteCloseParentheses();
                WriteNewLine();

                Write(body);


                EndBlock();
                WriteFinally();
                BeginBlock();
                Write(enumeratorVariable, ".dispose()");
                WriteSemiColon(true);
                EndBlock();
                EndBlock();
                EmitterContext.RecursiveForeach--;
            }
        }
    }
}
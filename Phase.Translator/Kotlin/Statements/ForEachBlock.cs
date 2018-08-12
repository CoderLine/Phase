using System;
using System.Threading;
using Microsoft.CodeAnalysis;
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

            if (foreachMode == ForeachMode.Native)
            {
                WriteFor();

                WriteOpenParentheses();

                WriteType(Node.Type);
                Write(" ", Node.Identifier.ValueText, " : ");
                EmitTree(Node.Expression, cancellationToken);

                WriteCloseParentheses();
                WriteNewLine();

                EmitTree(Node.Statement, cancellationToken);
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
                Write(")!!.getEnumerator()");
                WriteSemiColon(true);

                Write("try");
                BeginBlock();

                WriteWhile();
                WriteOpenParentheses();
                Write(enumeratorVariable, "!!.moveNext()");
                WriteCloseParentheses();
                WriteNewLine();
                BeginBlock();

                Write("var ", Node.Identifier.ValueText, " = ", enumeratorVariable, "!!.Current");
                WriteSemiColon(true);

                EmitTree(Node.Statement, cancellationToken);

                EndBlock();
                EndBlock();
                WriteFinally();
                BeginBlock();
                Write(enumeratorVariable, "!!.dispose()");
                WriteSemiColon(true);
                EndBlock();
                EndBlock();
                EmitterContext.RecursiveForeach--;
            }
        }
    }
}
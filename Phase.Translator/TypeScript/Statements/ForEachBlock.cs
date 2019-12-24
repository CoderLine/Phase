using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Attributes;

namespace Phase.Translator.TypeScript
{
    public class ForEachBlock : CommentedNodeEmitBlock<ForEachStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            EmitterContext.CurrentForIncrementors.Push(null);

            var type = Emitter.GetTypeInfo(Node.Expression);
            var foreachMode = Emitter.GetForeachMode(type.Type) ?? ForeachMode.AsIterable;

            WriteFor();
            WriteOpenParentheses();
            Write(Node.Identifier.ValueText);
            Write(" in ");

            switch (foreachMode)
            {
                case ForeachMode.AsIterable:
                    Write("new system.collections.generic.EnumerableIterable(");
                    EmitTree(Node.Expression, cancellationToken);
                    Write(")");
                    break;
                case ForeachMode.Native:
                    EmitTree(Node.Expression, cancellationToken);
                    break;
                case ForeachMode.GetEnumerator:
                    Write("(");
                    EmitTree(Node.Expression, cancellationToken);
                    Write(").getEnumerator()");
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unexpected foreachMode '" + foreachMode + "'");
            }
            WriteCloseParentheses();
            WriteNewLine();
            EmitTree(Node.Statement, cancellationToken);

            EmitterContext.CurrentForIncrementors.Pop();
        }
    }
}
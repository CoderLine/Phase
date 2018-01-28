using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Attributes;

namespace Phase.Translator.Haxe
{
    public class ForEachBlock : CommentedNodeEmitBlock<ForEachStatementSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var type = Emitter.GetTypeInfo(Node.Expression);
            var foreachMode = Emitter.GetForeachMode(type.Type);

            WriteFor();
            WriteOpenParentheses();
            Write(Node.Identifier.ValueText);
            Write(" in ");

            switch (foreachMode)
            {
                case ForeachMode.AsIterable:
                    Write("new system.EnumerableIterable(");
                    EmitTree(Node.Expression, cancellationToken);
                    Write(")");
                    break;
                case ForeachMode.Native:
                    EmitTree(Node.Expression, cancellationToken);
                    break;
                case ForeachMode.GetEnumerator:
                    Write("(");
                    EmitTree(Node.Expression, cancellationToken);
                    Write(").GetEnumerator()");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            WriteCloseParentheses();
            WriteNewLine();
            EmitTree(Node.Statement, cancellationToken);
        }
    }
}
using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Attributes;

namespace Phase.Translator.Cpp.Statements
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
                Write("auto &");
                Write(Node.Identifier.ValueText);
                Write(" : ");
                EmitTree(Node.Expression, cancellationToken);

                WriteCloseParentheses();
                WriteNewLine();
            }
            else
            {
                BeginBlock();

                var enumeratorVariable = "__e";
                var disposerVariable = "__d";
                if (EmitterContext.RecursiveForeach > 0)
                {
                    enumeratorVariable += EmitterContext.RecursiveForeach;
                    disposerVariable += EmitterContext.RecursiveForeach;
                }
                EmitterContext.RecursiveForeach++;

                Write("auto ", enumeratorVariable, " = ");
                Write("(");
                EmitTree(Node.Expression, cancellationToken);
                Write(")->GetEnumerator()");
                WriteSemiColon(true);

                Write("Phase::Disposer ", disposerVariable, "(", enumeratorVariable, ")");
                WriteSemiColon(true);

                WriteWhile();
                WriteOpenParentheses();
                Write(enumeratorVariable, "->MoveNext()");
                WriteCloseParentheses();
                WriteNewLine();
                BeginBlock();


                ITypeSymbol elementType = ((ILocalSymbol)Emitter.GetDeclaredSymbol(Node)).Type;
                WriteType(elementType);
                Write(" &", Node.Identifier.ValueText, " = ", enumeratorVariable, "->GetCurrent()");
                WriteSemiColon(true);

            }

            EmitTree(Node.Statement, cancellationToken);

            if (foreachMode != ForeachMode.Native)
            {
                EndBlock();
                EmitterContext.RecursiveForeach--;
                EndBlock();
            }
        }
    }
}
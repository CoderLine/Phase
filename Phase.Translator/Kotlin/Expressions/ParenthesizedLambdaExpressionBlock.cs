using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    class ParenthesizedLambdaExpressionBlock : AutoCastBlockBase<ParenthesizedLambdaExpressionSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            EmitterContext.AddLambdaNameForReturn();

            if (Node.ParameterList.Parameters.Count > 0)
            {
                WriteOpenBrace();

                for (int i = 0; i < Node.ParameterList.Parameters.Count; i++)
                {
                    if (i > 0)
                    {
                        WriteComma();
                    }
                    Write(Node.ParameterList.Parameters[i].Identifier.Text);

                    WriteColon();
                    var parameter = Emitter.GetDeclaredSymbol(Node.ParameterList.Parameters[i], cancellationToken);
                    Write(Emitter.GetTypeName(parameter.Type, false, false));
                }

                Write(" -> ");

                if (Node.Body is BlockSyntax block)
                {
                    PushWriter();
                    WriteOpenBrace();
                    WriteNewLine();
                    foreach (var statement in block.Statements)
                    {
                        EmitTree(statement, cancellationToken);
                    }
                    WriteCloseBrace();
                    var body = PopWriter();

                    if (EmitterContext.WasLambdaNameForReturnUsed(out var lambdaName))
                    {
                        Write("run ", lambdaName, "@");
                    }
                    Write(body);
                }
                else
                {
                    EmitTree(Node.Body, cancellationToken);
                }
            }
            else if (Node.Body is BlockSyntax block)
            {
                PushWriter();
                WriteOpenBrace();
                WriteNewLine();
                foreach (var statement in block.Statements)
                {
                    EmitTree(statement, cancellationToken);
                }
                var body = PopWriter();

                if (EmitterContext.WasLambdaNameForReturnUsed(out var lambdaName))
                {
                    Write("run ", lambdaName, "@");
                }
                Write(body);
            }
            else
            {
                WriteOpenBrace();
                EmitTree(Node.Body, cancellationToken);
            }

            WriteCloseBrace();
            EmitterContext.RemoveLambdaNameForReturn();


            return AutoCastMode.SkipCast;
        }
    }
}
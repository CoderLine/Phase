using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class AnonymousObjectCreationExpressionBlock : AbstractHaxeScriptEmitterBlock<AnonymousObjectCreationExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            WriteOpenBrace();
            WriteNewLine();
            EmitterContext.InitializerCount++;
            for (int i = 0; i < Node.Initializers.Count; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                    WriteNewLine();
                }

                var init = Node.Initializers[i];
                if (init.NameEquals != null)
                {
                    EmitTree(init.NameEquals.Name, cancellationToken);
                }
                else 
                {
                    var accessedMember = Emitter.GetSymbolInfo(init.Expression).Symbol;
                    switch (init.Expression.Kind())
                    {
                        case SyntaxKind.SimpleMemberAccessExpression:
                            if (accessedMember != null)
                            {
                                Write(Emitter.GetSymbolName(accessedMember));
                            }
                            else
                            {
                                Write(((MemberAccessExpressionSyntax) init.Expression).Name.Identifier.ValueText);
                            }
                            break;
                        case SyntaxKind.IdentifierName:
                            if (accessedMember != null)
                            {
                                Write(Emitter.GetSymbolName(accessedMember));
                            }
                            else
                            {
                                Write(((IdentifierNameSyntax)init.Expression).Identifier.ValueText);
                            }
                            break;
                        default:
                            throw new PhaseCompilerException($"Unknown expression kind '{init.Expression.Kind()} in anonymous object creation'");
                    }
                }
                WriteColon();
                WriteSpace();

                EmitTree(init.Expression, cancellationToken);
            }
            EmitterContext.InitializerCount--;
            WriteCloseBrace();
        }
    }
}
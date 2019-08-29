using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class ObjectCreationExpressionBlock : AbstractHaxeScriptEmitterBlock<ObjectCreationExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            string tmpvar = null;
            if (Node.Initializer != null)
            {
                WriteOpenParentheses();
                WriteFunction();
                WriteOpenCloseParentheses();
                BeginBlock();

                WriteVar();

                tmpvar = "_tmp";
                if (EmitterContext.RecursiveObjectCreation > 0)
                {
                    tmpvar += EmitterContext.RecursiveObjectCreation;
                }
                EmitterContext.RecursiveObjectCreation++;

                Write(tmpvar);
                Write(" = ");
            }

            var type = (ITypeSymbol)Emitter.GetSymbolInfo(Node.Type).Symbol;
            if (type.TypeKind == TypeKind.Delegate)
            {
                if (Node.ArgumentList.Arguments.Count == 1)
                {
                    EmitTree(Node.ArgumentList.Arguments[0], cancellationToken);
                }
            }
            else if (Emitter.HasNativeConstructors(type) || !Emitter.HasConstructorOverloads(type))
            {
                WriteNew();
                WriteType(Node.Type);
                var ctor = (IMethodSymbol)Emitter.GetSymbolInfo(Node).Symbol;
                WriteMethodInvocation(ctor, Node.ArgumentList, Node, cancellationToken);
            }
            else
            {
                WriteNew();
                WriteType(Node.Type);
                WriteOpenCloseParentheses();
                WriteDot();
                var ctor = (IMethodSymbol)Emitter.GetSymbolInfo(Node).Symbol;
                Write(Emitter.GetMethodName(ctor));
                WriteMethodInvocation(ctor, Node.ArgumentList, Node, cancellationToken);
            }

            if (Node.Initializer != null)
            {
                WriteSemiColon(true);
                EmitterContext.InitializerCount++;

                IMethodSymbol addMethod = null;
                if (Node.Initializer.Kind() == SyntaxKind.CollectionInitializerExpression)
                {
                    var memberType = type.AllInterfaces
                        .First(i => i.OriginalDefinition.Equals(
                            Emitter.GetPhaseType("System.Collections.Generic.IEnumerable`1")))
                        .TypeArguments[0];

                    addMethod = type.GetMembers("Add")
                        .OfType<IMethodSymbol>()
                        .First(m => m.Parameters.Length == 1 && m.Parameters[0].Type.Equals(memberType));
                }

                foreach (var expression in Node.Initializer.Expressions)
                {
                    if (expression.Kind() == SyntaxKind.SimpleAssignmentExpression)
                    {
                        var assignment = (AssignmentExpressionSyntax)expression;
                        var left = Emitter.GetSymbolInfo(assignment.Left);

                        Write(tmpvar);
                        WriteDot();
                        Write(Emitter.GetSymbolName(left.Symbol));

                        Write(" = ");

                        EmitTree(assignment.Right);

                        WriteSemiColon(true);
                    }
                    else if (Node.Initializer.Kind() == SyntaxKind.CollectionInitializerExpression)
                    {
                        Write(tmpvar);
                        WriteDot();
                        Write(Emitter.GetSymbolName(addMethod));
                        WriteMethodInvocation(addMethod, new[]
                        {
                            new ParameterInvocationInfo(expression)
                        }, null, cancellationToken);
                        WriteSemiColon(true);
                    }
                }

                EmitterContext.RecursiveObjectCreation--;
                EmitterContext.InitializerCount--;

                WriteReturn(true);
                Write(tmpvar);
                WriteSemiColon(true);

                EndBlock(false);
                WriteCloseParentheses();
                WriteOpenCloseParentheses();
            }
        }
    }
}
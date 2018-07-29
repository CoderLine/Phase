using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Expressions
{
    public class ObjectCreationExpressionBlock : AbstractJavaEmitterBlock<ObjectCreationExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            var type = (ITypeSymbol)Emitter.GetSymbolInfo(Node.Type).Symbol;

            if (type.TypeKind == TypeKind.Delegate)
            {
                throw new NotImplementedException("TODO: bind method or get pointer if static method");
                if (Node.ArgumentList.Arguments.Count == 1)
                {
                    EmitTree(Node.ArgumentList.Arguments[0], cancellationToken);
                }
            }

            if (Node.Initializer != null)
            {
                Write("system.Phase.initialize(");
            }

            var ctor = (IMethodSymbol)Emitter.GetSymbolInfo(Node).Symbol;
            var typeName = Emitter.GetTypeName(type, false, false);
            Write("new ", typeName);
            WriteMethodInvocation(ctor, Node.ArgumentList, Node, cancellationToken);

            if (Node.Initializer != null)
            {
                string tmpvar = "__this";
                if (EmitterContext.RecursiveObjectCreation > 0)
                {
                    tmpvar += EmitterContext.RecursiveObjectCreation;
                }
                EmitterContext.RecursiveObjectCreation++;

                Write(", (", typeName, " ", tmpvar, ") -> ");
                BeginBlock();

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

                        if (left.Symbol.Kind == SymbolKind.Property)
                        {
                            Write(tmpvar);
                            Write(".");
                            Write(Emitter.GetMethodName(((IPropertySymbol)left.Symbol).SetMethod));
                            WriteOpenParentheses();
                            EmitTree(assignment.Right);
                            WriteCloseParentheses();
                        }
                        else
                        {
                            Write(tmpvar);
                            Write(".");
                            Write(Emitter.GetSymbolName(left.Symbol));

                            Write(" = ");

                            EmitTree(assignment.Right);
                        }

                        WriteSemiColon(true);
                    }
                    else if (Node.Initializer.Kind() == SyntaxKind.CollectionInitializerExpression)
                    {
                        Write(tmpvar);
                        Write(".");
                        Write(Emitter.GetSymbolName(addMethod));
                        WriteMethodInvocation(addMethod, new[]
                        {
                            new ParameterInvocationInfo(expression)
                        }, null, cancellationToken);
                        WriteSemiColon(true);
                    }
                }

                EmitterContext.RecursiveObjectCreation--;

                EndBlock(false);
                WriteCloseParentheses();
            }
        }
    }
}
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    public class ObjectCreationExpressionBlock : AbstractKotlinEmitterBlock<ObjectCreationExpressionSyntax>
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

            var ctor = (IMethodSymbol)Emitter.GetSymbolInfo(Node).Symbol;
            var typeName = Emitter.GetTypeName(type, false, false, false);
            Write(typeName);
            WriteMethodInvocation(ctor, Node.ArgumentList, Node, cancellationToken);

            if (Node.Initializer != null)
            {
                WriteDot();
                Write("apply");
                BeginBlock();
                EmitterContext.RecursiveObjectCreation++;

                IMethodSymbol addMethod = null;
                if (Node.Initializer.Kind() == SyntaxKind.CollectionInitializerExpression)
                {
                    var memberType = type.AllInterfaces
                        .First(i => i.OriginalDefinition.Equals(
                            Emitter.GetPhaseType("System.Collections.Generic.IEnumerable`1")))
                        .TypeArguments[0];

                    var currentType = type;
                    while (addMethod == null && currentType != null && currentType.SpecialType != SpecialType.System_Object)
                    {
                        addMethod = currentType.GetMembers("Add")
                            .OfType<IMethodSymbol>()
                            .FirstOrDefault(m => m.Parameters.Length == 1 && m.Parameters[0].Type.Equals(memberType));
                        currentType = currentType.BaseType;
                    }
                }

                foreach (var expression in Node.Initializer.Expressions)
                {
                    if (expression.Kind() == SyntaxKind.SimpleAssignmentExpression)
                    {
                        var assignment = (AssignmentExpressionSyntax)expression;
                        var left = Emitter.GetSymbolInfo(assignment.Left);

                        Write("this.");
                        Write(EmitterContext.GetSymbolName(left.Symbol));

                        Write(" = ");

                        EmitTree(assignment.Right);

                        WriteNewLine();
                    }
                    else if (Node.Initializer.Kind() == SyntaxKind.CollectionInitializerExpression)
                    {
                        Write("this.");
                        Write(EmitterContext.GetSymbolName(addMethod));
                        WriteMethodInvocation(addMethod, new[]
                        {
                            new ParameterInvocationInfo(expression)
                        }, null, cancellationToken);
                        WriteSemiColon(true);
                    }
                }

                EmitterContext.RecursiveObjectCreation--;

                EndBlock(true);
            }
        }
    }
}
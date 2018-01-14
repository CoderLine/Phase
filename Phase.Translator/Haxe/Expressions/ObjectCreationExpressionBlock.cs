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
            var type = (ITypeSymbol) Emitter.GetSymbolInfo(Node.Type).Symbol;
            if (type.TypeKind == TypeKind.Delegate)
            {
                if (Node.ArgumentList.Arguments.Count == 1)
                {
                    EmitTree(Node.ArgumentList.Arguments[0], cancellationToken);
                }
            }
            else if(Emitter.HasNativeConstructors(type) || !Emitter.HasConstructorOverloads(type))
            {
                WriteNew();
                WriteType(Node.Type);
                var ctor = (IMethodSymbol)Emitter.GetSymbolInfo(Node).Symbol;
                WriteMethodInvocation(ctor, Node.ArgumentList, cancellationToken);
            }
            else
            {
                WriteNew();
                WriteType(Node.Type);
                WriteOpenCloseParentheses();
                WriteDot();
                var ctor = (IMethodSymbol)Emitter.GetSymbolInfo(Node).Symbol;
                Write(Emitter.GetMethodName(ctor));
                WriteMethodInvocation(ctor, Node.ArgumentList, cancellationToken);
            }
        }
    }
}
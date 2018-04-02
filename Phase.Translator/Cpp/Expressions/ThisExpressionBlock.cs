using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    class ThisExpressionBlock : AutoCastBlockBase<ThisExpressionSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (Node.Parent.Kind())
            {
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                    Write("this");
                    break;
                default:
                    Write("shared_from_base<");
                    var type = Emitter.GetTypeInfo(Node);
                    Write(Emitter.GetTypeName(type.Type, false, false, CppEmitter.TypeNamePointerKind.NoPointer));
                    Write(">");
                    WriteOpenCloseParentheses();
                    break;
            }

            return AutoCastMode.Default;
        }
    }
}
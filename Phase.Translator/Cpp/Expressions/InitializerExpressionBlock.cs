using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    public class InitializerExpressionBlock : AbstractCppEmitterBlock<InitializerExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var info = Emitter.GetTypeInfo(Node, cancellationToken);
            var type = (IArrayTypeSymbol)(info.Type ?? info.ConvertedType);
            var elementType = type.ElementType;
            var specialArray = Emitter.GetSpecialArrayName(elementType);

            if (specialArray != null)
            {
                Write(specialArray);
                Write("::Create");
            }
            else
            {
                Write(Emitter.GetTypeName(type, false, false, CppEmitter.TypeNamePointerKind.NoPointer));
            }

            WriteOpenParentheses();

            Write("std::initializer_list<");
            Write(Emitter.GetTypeName(elementType, false, false, CppEmitter.TypeNamePointerKind.SharedPointerDeclaration));
            Write("> {");

            for (var i = 0; i < Node.Expressions.Count; i++)
            {
                var expression = Node.Expressions[i];
                if(i > 0) WriteComma();
                EmitTree(expression, cancellationToken);
            }

            Write("}");

            WriteCloseParentheses();
        }
    }
}
﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    public class ArrayCreationExpressionBlock : AbstractCppEmitterBlock<ArrayCreationExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            var arrayType = (IArrayTypeSymbol)Emitter.GetTypeSymbol(Node.Type);
            var elementType = arrayType.ElementType;
            var specialArray = Emitter.GetSpecialArrayName(elementType);

            if (Node.Initializer == null)
            {
                if (specialArray != null && Node.Type.RankSpecifiers.Count == 1)
                {
                    Write(specialArray);
                    Write("::Empty");
                }
                else
                {
                    Write(Emitter.GetTypeName(arrayType, false, false, CppEmitter.TypeNamePointerKind.NoPointer));
                    Write("::");
                    Write("Empty");
                }

                WriteOpenParentheses();

                var x = 0;
                for (int i = 0; i < Node.Type.RankSpecifiers.Count; i++)
                {

                    for (int j = 0; j < Node.Type.RankSpecifiers[i].Sizes.Count; j++)
                    {
                        var expr = Node.Type.RankSpecifiers[i].Sizes[j];

                        if (expr.Kind() != SyntaxKind.OmittedArraySizeExpression)
                        {
                            if (x > 0)
                            {
                                WriteComma();
                            }
                            EmitTree(Node.Type.RankSpecifiers[i].Sizes[j], cancellationToken);
                            x++;
                        }
                    }
                }
                WriteCloseParentheses();
            }
            else
            {
                if (specialArray != null)
                {
                    Write(specialArray);
                    Write("::Create");
                }
                else
                {
                    Write(Emitter.GetTypeName(arrayType, false, false, CppEmitter.TypeNamePointerKind.NoPointer));
                }

                WriteOpenParentheses();

                Write("std::initializer_list<");
                Write(Emitter.GetTypeName(elementType, false, false, CppEmitter.TypeNamePointerKind.SharedPointerDeclaration));
                Write("> {");

                for (var i = 0; i < Node.Initializer.Expressions.Count; i++)
                {
                    var expression = Node.Initializer.Expressions[i];
                    if (i > 0) WriteComma();
                    EmitTree(expression, cancellationToken);
                }

                Write("}");

                WriteCloseParentheses();
            }

        }
    }
}
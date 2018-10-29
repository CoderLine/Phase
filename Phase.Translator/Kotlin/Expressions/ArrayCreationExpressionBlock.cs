using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    public class ArrayCreationExpressionBlock : AbstractKotlinEmitterBlock<ArrayCreationExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            var arrayType = (IArrayTypeSymbol)Emitter.GetTypeInfo(Node, cancellationToken).Type;
            ITypeSymbol elementType = arrayType;
            while (elementType is IArrayTypeSymbol)
            {
                elementType = ((IArrayTypeSymbol)elementType).ElementType;
            }

            if (Node.Initializer != null)
            {
                var multidimensional = Node.Type.RankSpecifiers.Count > 1 || Node.Type.RankSpecifiers[0].Sizes.Count > 1;
                if (multidimensional)
                {
                    Write("arrayOf(");
                }
                string arrayFunction = Emitter.GetArrayCreationFunctionName(elementType);
                if (multidimensional)
                {
                    for (var i = 0; i < Node.Initializer.Expressions.Count; i++)
                    {
                        var expression = Node.Initializer.Expressions[i];
                        if (i > 0) WriteComma();

                        if (expression is InitializerExpressionSyntax init)
                        {
                            Write(arrayFunction);
                            WriteOpenParentheses();

                            for (var j = 0; j < init.Expressions.Count; j++)
                            {
                                var itemExpr = Node.Initializer.Expressions[j];
                                if (j > 0) WriteComma();
                                EmitTree(itemExpr, cancellationToken);
                            }

                            WriteCloseParentheses();
                        }
                    }
                }
                else
                {
                    Write(arrayFunction);
                    WriteOpenParentheses();

                    for (var i = 0; i < Node.Initializer.Expressions.Count; i++)
                    {
                        var expression = Node.Initializer.Expressions[i];
                        if (i > 0) WriteComma();
                        EmitTree(expression, cancellationToken);
                    }

                    WriteCloseParentheses();
                }

                if (multidimensional)
                {
                    Write(")");
                }
            }
            else
            {
                if (Node.Type.RankSpecifiers.Count == 1)
                {
                    // int[] x = new int[2];
                    // int[,] y = new int[2,3];
                    // int[,,] z = new int[2,3,4];
                    // Test[,,] a = new Test[2,3,4];
                    // var x : IntArray = intArray(2);
                    // var y : Array<IntArray> = intMatrix2(2,3);
                    // var z : Array<Array<IntArray>> = intMatrix3(2,3,4);
                    // var a : Array<Array<Test?>> = matrixOf3<Test?>(2,3,4, emptyArray());
                    var ranks = Node.Type.RankSpecifiers[0];

                    if (ranks.Sizes.Count == 1)
                    {
                        string arrayClass = Emitter.GetSpecialArrayName(elementType);
                        if (arrayClass == null)
                        {
                            arrayClass = "arrayOfNulls<" + Emitter.GetTypeName(elementType, false, false, true) + ">";
                        }

                        Write(arrayClass);
                        WriteOpenParentheses();
                        EmitTree(Node.Type.RankSpecifiers[0].Sizes[0], cancellationToken);
                        WriteCloseParentheses();
                    }
                    else
                    {
                        bool emptyArrayParam = false;
                        switch (elementType.SpecialType)
                        {
                            case SpecialType.System_SByte:
                                Write("byteMatrix", ranks.Sizes.Count);
                                break;
                            case SpecialType.System_Byte:
                                Write("ubyteMatrix", ranks.Sizes.Count);
                                break;
                            case SpecialType.System_Int16:
                                Write("shortMatrix", ranks.Sizes.Count);
                                return;
                            case SpecialType.System_UInt16:
                                Write("ushortMatrix", ranks.Sizes.Count);
                                break;
                            case SpecialType.System_Int32:
                                Write("intMatrix", ranks.Sizes.Count);
                                break;
                            case SpecialType.System_UInt32:
                                Write("uintMatrix", ranks.Sizes.Count);
                                break;
                            case SpecialType.System_Int64:
                                Write("longMatrix", ranks.Sizes.Count);
                                break;
                            case SpecialType.System_UInt64:
                                Write("ulongMatrix", ranks.Sizes.Count);
                                break;
                            case SpecialType.System_Decimal:
                                Write("doubleMatrix", ranks.Sizes.Count);
                                break;
                            case SpecialType.System_Single:
                                Write("floatMatrix", ranks.Sizes.Count);
                                break;
                            case SpecialType.System_Double:
                                Write("doubleMatrix", ranks.Sizes.Count);
                                break;
                            case SpecialType.System_Boolean:
                                Write("booleanMatrix", ranks.Sizes.Count);
                                break;
                            case SpecialType.System_Char:
                                Write("charMatrix", ranks.Sizes.Count);
                                break;
                            default:
                                Write("matrixOf", ranks.Sizes.Count, "<",
                                    Emitter.GetTypeName(elementType, false, false, true), ">");
                                emptyArrayParam = true;
                                break;
                        }

                        WriteOpenParentheses();
                        for (int i = 0; i < ranks.Sizes.Count; i++)
                        {
                            if (i > 0) WriteComma();
                            EmitTree(ranks.Sizes[i], cancellationToken);
                        }
                        if (emptyArrayParam)
                        {
                            WriteComma();
                            Write("emptyArray()");
                        }


                        WriteCloseParentheses();
                    }
                }
                else
                {
                    // int[][] y = new int[2][];
                    // int[][] z = new int[2][3][];
                    // Test[][] a = new Test[2][3][];
                    // var y : Array<IntArray> = jaggedIntArray2<IntArray>(2);
                    // var z : Array<Array<IntArray>> = jaggedIntArray<IntArray>(2,3);
                    // var a : Array<Array<Test?>> = jaggedArrayOf<Test?>(2,3, emptyArray<Test?>());

                    bool emptyArrayParam = false;
                    switch (elementType.SpecialType)
                    {
                        case SpecialType.System_SByte:
                            Write("jaggedByteArray", Node.Type.RankSpecifiers.Count);
                            break;
                        case SpecialType.System_Byte:
                            Write("jaggedUByteArray", Node.Type.RankSpecifiers.Count);
                            break;
                        case SpecialType.System_Int16:
                            Write("jaggedShortArray", Node.Type.RankSpecifiers.Count);
                            return;
                        case SpecialType.System_UInt16:
                            Write("jaggedUShortArray", Node.Type.RankSpecifiers.Count);
                            break;
                        case SpecialType.System_Int32:
                            Write("jaggedIntArray", Node.Type.RankSpecifiers.Count);
                            break;
                        case SpecialType.System_UInt32:
                            Write("jaggedUIntArray", Node.Type.RankSpecifiers.Count);
                            break;
                        case SpecialType.System_Int64:
                            Write("jaggedLongArray", Node.Type.RankSpecifiers.Count);
                            break;
                        case SpecialType.System_UInt64:
                            Write("jaggedULongArray", Node.Type.RankSpecifiers.Count);
                            break;
                        case SpecialType.System_Decimal:
                            Write("jaggedDoubleArray", Node.Type.RankSpecifiers.Count);
                            break;
                        case SpecialType.System_Single:
                            Write("jaggedSingleArray", Node.Type.RankSpecifiers.Count);
                            break;
                        case SpecialType.System_Double:
                            Write("jaggedDoubleArray", Node.Type.RankSpecifiers.Count);
                            break;
                        case SpecialType.System_Boolean:
                            Write("jaggedBooleanArray", Node.Type.RankSpecifiers.Count);
                            break;
                        case SpecialType.System_Char:
                            Write("jaggedCharArray", Node.Type.RankSpecifiers.Count);
                            break;
                        default:
                            Write("jaggedArrayOf", Node.Type.RankSpecifiers.Count, "<",
                                Emitter.GetTypeName(elementType, false, false, true), ">");
                            emptyArrayParam = true;
                            break;
                    }

                    WriteOpenParentheses();
                    // last one is always empty, 
                    for (int i = 0; i < Node.Type.RankSpecifiers.Count - 1; i++)
                    {
                        if (i > 0) WriteComma();
                        var ranks = Node.Type.RankSpecifiers[i];
                        if (ranks.Sizes.Count != 1)
                        {
                            throw new PhaseCompilerException("Mixing of jagged and matrix arrays not supported yet");
                        }
                        EmitTree(ranks.Sizes[i], cancellationToken);
                    }

                    if (emptyArrayParam)
                    {
                        WriteComma();
                        Write("emptyArray()");
                    }

                    WriteCloseParentheses();
                }
            }
        }
    }
}
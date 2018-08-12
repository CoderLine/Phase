using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
{
    public class ArrayCreationExpressionBlock : AbstractKotlinEmitterBlock<ArrayCreationExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            var multidimensional = Node.Type.RankSpecifiers.Count > 1 || Node.Type.RankSpecifiers[0].Sizes.Count > 1;
            if (multidimensional)
            {
                Write("arrayOf(");
            }

            var arrayType = (IArrayTypeSymbol)Emitter.GetTypeInfo(Node, cancellationToken).Type;
            ITypeSymbol elementType = arrayType;
            while (elementType is IArrayTypeSymbol)
            {
                elementType = ((IArrayTypeSymbol)elementType).ElementType;
            }

            if (Node.Initializer != null)
            {
                string arrayFunction;
                switch (elementType.SpecialType)
                {
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                        arrayFunction = "byteArrayOf";
                        break;
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                        arrayFunction = "shortArrayOf";
                        break;
                    case SpecialType.System_Int32:
                    case SpecialType.System_UInt32:
                        arrayFunction = "intArrayOf";
                        break;
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt64:
                        arrayFunction = "longArrayOf";
                        break;
                    case SpecialType.System_Boolean:
                        arrayFunction = "booleanArrayOf";
                        break;
                    case SpecialType.System_Char:
                        arrayFunction = "charArrayOf";
                        break;
                    case SpecialType.System_Single:
                        arrayFunction = "floatArrayOf";
                        break;
                    case SpecialType.System_Decimal:
                    case SpecialType.System_Double:
                        arrayFunction = "doubleArrayOf";
                        break;
                    default:
                        arrayFunction = "arrayOf";
                        break;
                }

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

            }
            else
            {
                string arrayClass = Emitter.GetSpecialArrayName(elementType);
                if (arrayClass == null)
                {
                    arrayClass = "arrayOfNulls<" + Emitter.GetTypeName(elementType, false, false, true) + ">";
                }

                // int[,][,,] x = new int[2,3][7,8];
                // var x : Array<Array<IntArray>> = 
                //             arrayOf(
                //                  arrayOf(
                //
                for (int i = 0; i < Node.Type.RankSpecifiers.Count; i++)
                {
                    if (i > 0) WriteComma();

                    if (Node.Type.RankSpecifiers[i].Sizes.Count > 1)
                    {
                        Write("arrayOf(");

                        for (int j = 0; j < Node.Type.RankSpecifiers[i].Sizes.Count; j++)
                        {
                            if (j > 0) WriteComma();
                            Write(arrayClass);
                            WriteOpenParentheses();
                            EmitTree(Node.Type.RankSpecifiers[i].Sizes[0], cancellationToken);
                            WriteCloseParentheses();
                        }

                        Write(")");
                    }
                    else if (Node.Type.RankSpecifiers[i].Sizes.Count == 1)
                    {
                        Write(arrayClass);
                        WriteOpenParentheses();
                        EmitTree(Node.Type.RankSpecifiers[i].Sizes[0], cancellationToken);
                        WriteCloseParentheses();
                    }
                    else
                    {
                        Debugger.Break();
                    }
                }
            }

            if (multidimensional)
            {
                Write(")");
            }
        }
    }
}
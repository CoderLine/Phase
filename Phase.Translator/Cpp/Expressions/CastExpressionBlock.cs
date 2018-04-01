using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp.Expressions
{
    class CastExpressionBlock : AutoCastBlockBase<CastExpressionSyntax>
    {
        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            var sourceType = Emitter.GetTypeInfo(Node.Expression);
            var targetType = Emitter.GetTypeInfo(Node);
            EmitterContext.ImportType(targetType.Type);

            if (sourceType.Type.SpecialType == SpecialType.System_Object)
            {
                switch (targetType.Type.SpecialType)
                {
                    case SpecialType.System_String:
                        {
                            var extensionsName = Emitter.GetTypeName(targetType.Type, false, true,
                                CppEmitter.TypeNamePointerKind.NoPointer);
                            Write(extensionsName, "::Unbox(");
                            EmitTree(Node.Expression, cancellationToken);
                            WriteCloseParentheses();
                        }
                        return AutoCastMode.Default;
                    case SpecialType.System_Boolean:
                    case SpecialType.System_Char:
                    case SpecialType.System_Byte:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                        {
                            var extensionsName =
                                Emitter.GetTypeName(targetType.Type, false, true, CppEmitter.TypeNamePointerKind.NoPointer) +
                                "Extensions";
                            Write(extensionsName, "::Unbox(");
                            EmitTree(Node.Expression, cancellationToken);
                            WriteCloseParentheses();
                        }
                        return AutoCastMode.Default;
                }


            }

            if (targetType.Type.IsReferenceType && targetType.Type.SpecialType != SpecialType.System_String)
            {
                Write("std::static_pointer_cast<");
                Write(Emitter.GetTypeName(targetType.Type, false, false, CppEmitter.TypeNamePointerKind.NoPointer));
                Write(">(");
                EmitTree(Node.Expression, cancellationToken);
                Write(")");
            }
            else
            {
                Write("(");
                WriteType(targetType.Type);
                Write(")");
                EmitTree(Node.Expression, cancellationToken);
                return AutoCastMode.AddParenthesis;
            }
            return AutoCastMode.Default;
        }
    }
}
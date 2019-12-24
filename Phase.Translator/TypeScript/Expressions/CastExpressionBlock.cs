using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Attributes;

namespace Phase.Translator.TypeScript.Expressions
{
    class CastExpressionBlock : AbstractTypeScriptEmitterBlock<CastExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            var sourceType = Emitter.GetTypeInfo(Node.Expression);
            var targetType = Emitter.GetTypeInfo(Node);

            switch (targetType.Type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    if (sourceType.Type.Equals(targetType.Type))
                    {
                        EmitTree(Node.Expression, cancellationToken);
                    }
                    else if (Emitter.IsIConvertible(sourceType.Type))
                    {
                        if(Emitter.AreTypeMethodsRedirected(sourceType.Type, out var redirect))
                        {
                            Write(redirect);
                            WriteDot();
                            Write("to" + targetType.Type.Name);
                            WriteOpenParentheses();
                            EmitTree(Node.Expression, cancellationToken);
                            WriteCloseParentheses();
                        }
                        else if(sourceType.Type.TypeKind == TypeKind.Enum)
                        {
                            WriteType(sourceType.Type);
                            WriteDot();
                            Write("to" + targetType.Type.Name);
                            WriteOpenParentheses();
                            EmitTree(Node.Expression, cancellationToken);
                            WriteCloseParentheses();
                        }
                        else
                        {
                            WriteOpenParentheses();
                            EmitTree(Node.Expression, cancellationToken);
                            WriteCloseParentheses();
                            WriteDot();
                            Write("to" + targetType.Type.Name);
                            WriteOpenParentheses();
                            WriteCloseParentheses();
                        }
                    }
                    else
                    {
                        Write("untyped ");
                        EmitTree(Node.Expression, cancellationToken);
                    }
                    return;
            }

            if (targetType.Type.TypeKind == TypeKind.Delegate)
            {
                EmitTree(Node.Expression, cancellationToken);
            }
            else
            {
                var castMode = Emitter.GetCastMode(targetType.Type);
                switch (castMode)
                {
                    case CastMode.SafeCast:
                    case CastMode.UnsafeCast:
                    case CastMode.Untyped:
                        WriteOpenParentheses();
                        EmitTree(Node.Expression, cancellationToken);
                        Write(" as ");
                        WriteType(targetType.Type);
                        WriteCloseParentheses();
                        break;
                    case CastMode.Ignore:
                        EmitTree(Node.Expression, cancellationToken);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private bool IsSpecialArray(ITypeSymbol type)
        {
            return type.TypeKind == TypeKind.Array &&
                   Emitter.GetSpecialArrayName(((IArrayTypeSymbol)type).ElementType) != null;
        }
    }
}
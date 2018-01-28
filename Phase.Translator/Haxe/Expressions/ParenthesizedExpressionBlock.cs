using System.Threading;
using Haxe;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class ParenthesizedExpressionBlock : AbstractHaxeScriptEmitterBlock<ParenthesizedExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteOpenParentheses();
            EmitTree(Node.Expression, cancellationToken);
            WriteCloseParentheses();

            var typeInfo = Emitter.GetTypeInfo(Node, cancellationToken);
            // implicit cast
            if (typeInfo.ConvertedType != null && typeInfo.Type != null && !typeInfo.Type.Equals(typeInfo.ConvertedType))
            {
                switch (typeInfo.ConvertedType.SpecialType)
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
                        if (Emitter.IsIConvertible(typeInfo.Type))
                        {
                            WriteDot();
                            Write("To" + typeInfo.ConvertedType.Name + "_IFormatProvider");
                            WriteOpenParentheses();
                            Write("null");
                            WriteCloseParentheses();
                        }
                        return;
                }

                if (typeInfo.ConvertedType.Equals(Emitter.GetPhaseType("Haxe.HaxeInt")))
                {
                    switch (typeInfo.Type.SpecialType)
                    {
                        case SpecialType.System_Byte:
                        case SpecialType.System_SByte:
                        case SpecialType.System_Int16:
                        case SpecialType.System_Int32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt16:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_UInt64:
                            WriteDot();
                            Write("ToHaxeInt()");
                            return;
                    }

                }

                if (typeInfo.ConvertedType.Equals(Emitter.GetPhaseType("Haxe.HaxeFloat")))
                {
                    switch (typeInfo.Type.SpecialType)
                    {
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
                            WriteDot();
                            Write("ToHaxeFloat()");
                            return;
                    }
                }

                if (typeInfo.Type.SpecialType == SpecialType.System_String && typeInfo.ConvertedType.Equals(Emitter.GetPhaseType("Haxe.HaxeString")))
                {
                    WriteDot();
                    Write("ToHaxeString()");
                }
            }

        }
    }
}

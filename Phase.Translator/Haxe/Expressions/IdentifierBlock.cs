using System;
using System.Threading;
using System.Threading.Tasks;
using Haxe;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class IdentifierBlock : AbstractHaxeScriptEmitterBlock<IdentifierNameSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            var resolve = Emitter.GetSymbolInfo(Node);
            if (resolve.Symbol == null)
            {
                Write(Node.Identifier.Text);
            }
            else
            {
                if (resolve.Symbol is ITypeSymbol)
                {
                    WriteType((ITypeSymbol)resolve.Symbol);
                }
                else
                {
                    if (resolve.Symbol.IsStatic &&
                        !resolve.Symbol.ContainingType.Equals(EmitterContext.CurrentType.TypeSymbol))
                    {
                        WriteType(resolve.Symbol.ContainingType);
                        WriteDot();
                    }
                    Write(resolve.Symbol.Name);
                }

                if (!EmitterContext.IsMethodInvocation && (
                    (resolve.Symbol.Kind == SymbolKind.Local && Emitter.IsRefVariable((ILocalSymbol)resolve.Symbol)) ||
                    (resolve.Symbol.Kind == SymbolKind.Parameter && ((IParameterSymbol)resolve.Symbol).RefKind != RefKind.None)
                ))
                {
                    WriteDot();
                    Write("Value");
                }
            }

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
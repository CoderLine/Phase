using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Translator.Utils;

namespace Phase.Translator.Haxe.Expressions
{
    public class AssignmentExpressionBlock : AbstractHaxeScriptEmitterBlock<AssignmentExpressionSyntax>
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            var leftSymbol = Emitter.GetSymbolInfo(Node.Left);
            var leftType = Emitter.GetTypeInfo(Node.Left);
            var rightType = Emitter.GetTypeInfo(Node.Right);

            if (leftSymbol.Symbol is IPropertySymbol prop && prop.SetMethod != null)
            {
                var template = Emitter.GetTemplate(prop.SetMethod);
                if (template != null)
                {
                    if (template.Variables.TryGetValue("this", out var thisVar))
                    {
                        PushWriter();
                        if (leftSymbol.Symbol.IsStatic)
                        {
                            Write(Emitter.GetTypeName(leftSymbol.Symbol.ContainingType, false, true));
                        }
                        else
                        {
                            EmitTree(Node.Left, cancellationToken);
                        }

                        thisVar.RawValue = PopWriter();
                    }

                    if (template.Variables.TryGetValue("value", out var variable))
                    {
                        PushWriter();
                        EmitValue(leftType.Type, rightType.Type, cancellationToken);
                        variable.RawValue = PopWriter();
                    }

                    Write(template.ToString());
                    return;
                }
            }


            var op = GetOperator();
            if (leftSymbol.Symbol != null && leftSymbol.Symbol.Kind == SymbolKind.Property && ((IPropertySymbol)leftSymbol.Symbol).IsIndexer &&
                !Emitter.IsNativeIndexer(leftSymbol.Symbol))
            {
                EmitTree(Node.Left, cancellationToken);

                WriteComma();

                if (!string.IsNullOrEmpty(op))
                {
                    var property = ((IPropertySymbol)leftSymbol.Symbol);
                    if (Node.Left is ElementAccessExpressionSyntax elementAccess)
                    {
                        EmitTree(elementAccess.Expression, cancellationToken);
                        WriteDot();
                        Write(Emitter.GetMethodName(property.GetMethod));
                        WriteOpenParentheses();

                        for (int i = 0; i < elementAccess.ArgumentList.Arguments.Count; i++)
                        {
                            if (i > 0)
                            {
                                WriteComma();
                            }
                            EmitTree(elementAccess.ArgumentList.Arguments[i], cancellationToken);
                        }

                        WriteCloseParentheses();

                        WriteSpace();
                        Write(op);
                        WriteSpace();
                    }
                }

                EmitValue(leftType.Type, rightType.Type, cancellationToken);

                WriteCloseParentheses();
            }
            else if (leftSymbol.Symbol != null && leftSymbol.Symbol.Kind == SymbolKind.Event)
            {
                IEventSymbol evt = (IEventSymbol)leftSymbol.Symbol;
                IMethodSymbol method;
                switch (Node.Kind())
                {
                    case SyntaxKind.AddAssignmentExpression:
                        method = evt.AddMethod;
                        break;
                    case SyntaxKind.SubtractAssignmentExpression:
                        method = evt.RemoveMethod;
                        break;
                    default:
                        method = null;
                        break;
                }

                if (method != null)
                {
                    if (Node.Left is MemberAccessExpressionSyntax member)
                    {
                        EmitTree(member.Expression, cancellationToken);
                        WriteDot();
                    }
                    Write(Emitter.GetMethodName(method));
                    WriteMethodInvocation(method, new[]
                    {
                        new ParameterInvocationInfo(Node.Right)
                    }, Node, cancellationToken);
                }
            }
            else
            {
                EmitterContext.IsAssignmentLeftHand = true;
                EmitTree(Node.Left, cancellationToken);
                EmitterContext.IsAssignmentLeftHand = false;
                Write(" = ");

                var needsConversion = NeedsConversion(leftType, rightType, op);

                if (needsConversion)
                {
                    WriteOpenParentheses();
                }

                if (!string.IsNullOrEmpty(op))
                {
                    EmitTree(Node.Left, cancellationToken);
                    WriteSpace();
                    Write(op);
                    WriteSpace();
                    WriteOpenParentheses();
                }
                EmitValue(leftType.Type, rightType.Type, cancellationToken);
                if (!string.IsNullOrEmpty(op))
                {
                    WriteCloseParentheses();
                }
                if (needsConversion)
                {
                    WriteCloseParentheses();
                    if (Emitter.IsIConvertible(rightType.Type))
                    {
                        WriteDot();
                        Write("to" + leftType.Type.Name + "_IFormatProvider");
                        WriteOpenParentheses();
                        Write("null");
                        WriteCloseParentheses();
                    }
                }
            }
        }

        private void EmitValue(ITypeSymbol leftType, ITypeSymbol rightType, CancellationToken cancellationToken)
        {
            if (Node.Right is LiteralExpressionSyntax)
            {
                EmitTree(Node.Right, cancellationToken);
                return;
            }

            PushWriter();
            var block = EmitTree(Node.Right, cancellationToken);
            var result = PopWriter();

            var mode = block is IAutoCastBlock ? AutoCastMode.SkipCast : AutoCastMode.AddParenthesis;

            WriteWithAutoCast(mode, leftType, rightType, result);
        }

        private bool NeedsConversion(TypeInfo leftType, TypeInfo rightType, string op)
        {
            if (leftType.Type == null || rightType.Type == null)
            {
                return false;
            }

            if (leftType.Type.SpecialType == rightType.Type.SpecialType)
            {
                switch (leftType.Type.SpecialType)
                {
                    case SpecialType.System_Boolean:
                    case SpecialType.System_Char:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt64:
                        return !string.IsNullOrEmpty(op);
                }
            }

            return false;
        }

        private string GetOperator()
        {
            var op = "";
            switch (Node.Kind())
            {
                case SyntaxKind.OrAssignmentExpression:
                    op = "|";
                    break;
                case SyntaxKind.AndAssignmentExpression:
                    op = "&";
                    break;
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                    op = "^";
                    break;
                case SyntaxKind.LeftShiftAssignmentExpression:
                    op = "<<";
                    break;
                case SyntaxKind.RightShiftAssignmentExpression:
                    op = ">>";
                    break;
                case SyntaxKind.AddAssignmentExpression:
                    op = "+";
                    break;
                case SyntaxKind.SubtractAssignmentExpression:
                    op = "-";
                    break;
                case SyntaxKind.MultiplyAssignmentExpression:
                    op = "*";
                    break;
                case SyntaxKind.DivideAssignmentExpression:
                    op = "/";
                    break;
                case SyntaxKind.ModuloAssignmentExpression:
                    op = "%";
                    break;
            }

            return op;
        }
    }
}
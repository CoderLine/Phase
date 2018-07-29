﻿using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java.Expressions
{
    public class AssignmentExpressionBlock : AbstractJavaEmitterBlock<AssignmentExpressionSyntax>
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
            if (leftSymbol.Symbol != null && leftSymbol.Symbol.Kind == SymbolKind.Property &&
                !Emitter.IsNativeIndexer(leftSymbol.Symbol))
            {
                EmitTree(Node.Left, cancellationToken);
                var property = ((IPropertySymbol)leftSymbol.Symbol);

                if (property.IsIndexer)
                {
                    WriteComma();
                }


                if (!string.IsNullOrEmpty(op))
                {
                    if (Node.Left is ElementAccessExpressionSyntax elementAccess)
                    {
                        EmitTree(elementAccess.Expression, cancellationToken);
                        WriteDot();
                        Write(Emitter.GetMethodName(property.GetMethod));
                        WriteOpenParentheses();

                        for (int i = 0; i < elementAccess.ArgumentList.Arguments.Count; i++)
                        {
                            if (i > 0 && property.IsIndexer)
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

                if (property.SetMethod != null)
                {
                    WriteCloseParentheses();
                }
            }
            else if (leftSymbol.Symbol != null && leftSymbol.Symbol.Kind == SymbolKind.Event)
            {
                switch (Node.Left)
                {
                    case MemberAccessExpressionSyntax memberAccess:
                        EmitTree(memberAccess.Expression, cancellationToken);
                        WriteDot();
                        break;
                    case IdentifierNameSyntax identifier:
                        break;
                }

                string name;
                switch (Node.Kind())
                {
                    case SyntaxKind.AddAssignmentExpression:
                        name = Emitter.GetMethodName(((IEventSymbol)leftSymbol.Symbol).AddMethod);
                        Write(name);
                        break;
                    case SyntaxKind.SubtractAssignmentExpression:
                        name = Emitter.GetMethodName(((IEventSymbol)leftSymbol.Symbol).RemoveMethod);
                        Write(name);
                        break;
                }
                WriteOpenParentheses();
                EmitValue(leftType.Type, rightType.Type, cancellationToken);
                WriteCloseParentheses();
            }
            else
            {
                EmitTree(Node.Left, cancellationToken);
                Write(" = ");

                var fromValue = leftType.Type?.TypeKind == TypeKind.Enum && rightType.Type?.TypeKind != TypeKind.Enum;

                if (fromValue)
                {
                    WriteType(leftType.Type);
                    Write(".fromValue(");
                }

                if (!string.IsNullOrEmpty(op))
                {
                    EmitTree(Node.Left, cancellationToken);
                    if (leftType.Type?.TypeKind == TypeKind.Enum)
                    {
                        Write(".getValue()");
                    }
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


                if (fromValue)
                {
                    WriteCloseParentheses();
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
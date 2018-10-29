using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Translator.Utils;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Phase.Translator.Cpp.Expressions
{
    public class MemberAccessExpressionBlock : AutoCastBlockBase<MemberAccessExpressionSyntax>
    {
        public bool SkipSemicolonOnStatement { get; set; }

        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            var member = Emitter.GetSymbolInfo(Node);

            if (member.Symbol != null)
            {
                CodeTemplate template;
                if (member.Symbol is IPropertySymbol property && property.GetMethod != null)
                {
                    template = Emitter.GetTemplate(property.GetMethod);
                }
                else
                {
                    template = Emitter.GetTemplate(member.Symbol);
                }

                if (template != null)
                {
                    SkipSemicolonOnStatement = template.SkipSemicolonOnStatements;
                    if (template.Variables.TryGetValue("this", out var thisVar))
                    {
                        PushWriter();
                        if (member.Symbol.IsStatic)
                        {
                            Write(Emitter.GetTypeName(member.Symbol.ContainingType, false, true));
                        }
                        else
                        {
                            EmitTree(Node.Expression, cancellationToken);
                        }

                        thisVar.RawValue = PopWriter();
                    }

                    Write(template.ToString());
                    return AutoCastMode.SkipCast;
                }
            }

            if (member.Symbol != null && member.Symbol.Kind == SymbolKind.Property && !Emitter.IsNativeIndexer(member.Symbol))
            {
                var property = ((IPropertySymbol)member.Symbol);
                EmitTree(Node.Expression, cancellationToken);
                if (member.Symbol.IsStatic)
                {
                    Write("::");
                }
                else
                {
                    Write("->");
                }
                EmitterContext.ImportType(((IPropertySymbol)member.Symbol).Type);

                var writeCloseParenthesis = false;
                switch (Node.Parent.Kind())
                {
                    case SyntaxKind.AddAssignmentExpression:
                    case SyntaxKind.AndAssignmentExpression:
                    case SyntaxKind.DivideAssignmentExpression:
                    case SyntaxKind.ExclusiveOrAssignmentExpression:
                    case SyntaxKind.LeftShiftAssignmentExpression:
                    case SyntaxKind.ModuloAssignmentExpression:
                    case SyntaxKind.MultiplyAssignmentExpression:
                    case SyntaxKind.OrAssignmentExpression:
                    case SyntaxKind.RightShiftAssignmentExpression:
                    case SyntaxKind.SimpleAssignmentExpression:
                    case SyntaxKind.SubtractAssignmentExpression:
                        if (((AssignmentExpressionSyntax)Node.Parent).Left == Node)
                        {
                            Write(Emitter.GetMethodName(property.SetMethod));
                            WriteOpenParentheses();
                        }
                        else
                        {
                            Write(Emitter.GetMethodName(property.GetMethod));
                            WriteOpenParentheses();
                            writeCloseParenthesis = true;
                        }
                        break;
                    case SyntaxKind.PreIncrementExpression:
                    case SyntaxKind.PreDecrementExpression:
                    case SyntaxKind.PostIncrementExpression:
                    case SyntaxKind.PostDecrementExpression:
                        Write(Emitter.GetMethodName(property.SetMethod));
                        WriteOpenParentheses();
                        break;
                    default:
                        Write(Emitter.GetMethodName(property.GetMethod));
                        WriteOpenParentheses();
                        writeCloseParenthesis = true;
                        break;
                }

                if (writeCloseParenthesis)
                {
                    WriteCloseParentheses();
                }

                return AutoCastMode.Default;
            }

            var expression = Node.Expression;
            var leftHandSide = Emitter.GetSymbolInfo(expression);
            if (member.Symbol != null 
                && member.Symbol is IFieldSymbol constField 
                && constField.ContainingType.TypeKind != TypeKind.Enum
                && constField.IsConst 
                && (constField.DeclaringSyntaxReferences.Length == 0 || EmitterContext.IsCaseLabel))
            {
                return WriteConstant(constField);
            }

            if (leftHandSide.Symbol == null)
            {
                EmitTree(expression, cancellationToken);
            }
            else
            {
                var kind = leftHandSide.Symbol.Kind;
                switch (kind)
                {
                    case SymbolKind.NamedType:
                        Write(Emitter.GetTypeName((INamedTypeSymbol)leftHandSide.Symbol, false, false, CppEmitter.TypeNamePointerKind.NoPointer));
                        EmitterContext.ImportType((INamedTypeSymbol)leftHandSide.Symbol);
                        break;
                    default:
                        EmitTree(expression, cancellationToken);
                        break;
                }
            }

            
            if (member.Symbol == null)
            {
                Write("->");
                Write(Node.Name.Identifier);
            }
            else
            {
                if (member.Symbol.IsStatic || Node.Expression.Kind() == SyntaxKind.BaseExpression)
                {
                    Write("::");
                }
                else
                {
                    Write("->");
                }

                switch (member.Symbol.Kind)
                {
                    case SymbolKind.Property:
                        var getMethod = ((IPropertySymbol)member.Symbol).GetMethod;
                        EmitterContext.ImportType(((IPropertySymbol)member.Symbol).Type);
                        Write(Emitter.GetMethodName(getMethod));
                        WriteMethodInvocation(getMethod, new ParameterInvocationInfo[0], Node, cancellationToken);
                        break;
                    case SymbolKind.Field:
                        Write(EmitterContext.GetSymbolName(member.Symbol));
                        EmitterContext.ImportType(((IFieldSymbol)member.Symbol).Type);
                        break;
                    default:
                        Write(EmitterContext.GetSymbolName(member.Symbol));
                        break;
                }
            }

            return AutoCastMode.Default;
        }
    }
}
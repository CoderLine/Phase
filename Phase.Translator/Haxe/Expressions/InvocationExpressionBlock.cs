using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe.Expressions
{
    public class InvocationExpressionBlock : AbstractHaxeScriptEmitterBlock<InvocationExpressionSyntax>
    {
        public bool SkipSemicolonOnStatement { get; set; }

        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Node.Expression is IdentifierNameSyntax)
            {
                var value = Emitter.GetConstantValue(Node);
                if (value.HasValue)
                {
                    Write(value.Value);
                    return;
                }
            }

            var symbol = Emitter.GetSymbolInfo(Node.Expression).Symbol;
            var methodSymbol = symbol as IMethodSymbol;
            if (methodSymbol == null && symbol != null)
            {
                switch (symbol.Kind)
                {
                    case SymbolKind.Parameter:
                        if (((IParameterSymbol)symbol).Type != null &&
                            ((IParameterSymbol)symbol).Type.TypeKind == TypeKind.Delegate)
                        {
                            methodSymbol = ((INamedTypeSymbol)((IParameterSymbol)symbol).Type).DelegateInvokeMethod;
                        }
                        break;
                    case SymbolKind.Property:
                        if (((IPropertySymbol)symbol).Type != null &&
                            ((IPropertySymbol)symbol).Type.TypeKind == TypeKind.Delegate)
                        {
                            methodSymbol = ((INamedTypeSymbol)((IPropertySymbol)symbol).Type).DelegateInvokeMethod;
                        }
                        break;
                    case SymbolKind.Field:
                        if (((IFieldSymbol)symbol).Type != null &&
                            ((IFieldSymbol)symbol).Type.TypeKind == TypeKind.Delegate)
                        {
                            methodSymbol = ((INamedTypeSymbol)((IFieldSymbol)symbol).Type).DelegateInvokeMethod;
                        }
                        break;
                    case SymbolKind.Local:
                        if (((ILocalSymbol)symbol).Type != null &&
                            ((ILocalSymbol)symbol).Type.TypeKind == TypeKind.Delegate)
                        {
                            methodSymbol = ((INamedTypeSymbol)((ILocalSymbol)symbol).Type).DelegateInvokeMethod;
                        }
                        break;
                }
            }

            if (methodSymbol != null)
            {
                if (Emitter.IsPhaseClass(methodSymbol.ContainingType))
                {
                    switch (methodSymbol.Name)
                    {
                        case "Write":
                            var expr = Node.ArgumentList.Arguments.First().Expression;
                            switch (expr.Kind())
                            {
                                case SyntaxKind.StringLiteralExpression:
                                    Write(((LiteralExpressionSyntax) expr).Token.Value);
                                    break;
                                default:
                                    // TODO: report compilation error or warning
                                    break;
                            }
                            SkipSemicolonOnStatement = true;
                            break;
                        case "As":
                            Write("cast (");
                            switch (Node.Expression.Kind())
                            {
                                case SyntaxKind.SimpleMemberAccessExpression:
                                    EmitTree(((MemberAccessExpressionSyntax)Node.Expression).Expression, cancellationToken);
                                    break;
                                default:
                                    // TODO: report compilation error or warning
                                    Debug.Fail("Unknown expression for extension method");
                                    break;
                            }
                            Write(")");
                            break;
                    }
                }
                else if (NeedsSpecialTypeExtension(methodSymbol))
                {
                    Write(Emitter.GetTypeName(methodSymbol.ContainingType, false, true));
                    Write("Extensions");
                    WriteDot();
                    Write(Emitter.GetMethodName(methodSymbol));
                    if (methodSymbol.IsStatic)
                    {
                        WriteMethodInvocation(methodSymbol, Node.ArgumentList, null,
                            cancellationToken);
                    }
                    else
                    {
                        WriteMethodInvocation(methodSymbol, Node.ArgumentList, GetInvokeExpression(Node.Expression),
                            cancellationToken);
                    }
                }
                else if (methodSymbol.IsExtensionMethod)
                {
                    Write(Emitter.GetTypeName(methodSymbol.ContainingType, false, true));
                    WriteDot();
                    Write(Emitter.GetMethodName(methodSymbol));
                    WriteMethodInvocation(methodSymbol, Node.ArgumentList, GetInvokeExpression(Node.Expression),
                        cancellationToken);
                }
                else if (methodSymbol.IsStatic)
                {
                    Write(Emitter.GetTypeName(methodSymbol.ContainingType, false, true));
                    WriteDot();
                    Write(Emitter.GetMethodName(methodSymbol));
                    WriteMethodInvocation(methodSymbol, Node.ArgumentList, null,
                        cancellationToken);
                }
                else
                {
                    if (methodSymbol.MethodKind == MethodKind.DelegateInvoke)
                    {
                        EmitTree(Node.Expression, cancellationToken);
                    }
                    else
                    {
                        switch (Node.Expression.Kind())
                        {
                            case SyntaxKind.SimpleMemberAccessExpression:
                                EmitTree(((MemberAccessExpressionSyntax)Node.Expression).Expression, cancellationToken);
                                WriteDot();
                                break;
                            case SyntaxKind.ElementAccessExpression:
                                EmitTree(Node.Expression, cancellationToken);
                                WriteDot();
                                break;
                            case SyntaxKind.IdentifierName:
                                break;
                            default:
                                Debug.Fail("Unknown ezxpression for method invocation");
                                break;
                        }

                        //await EmitTreeAsync(Node.Expression, cancellationToken);
                        Write(Emitter.GetMethodName(methodSymbol));
                    }
                  
                    WriteMethodInvocation(methodSymbol, Node.ArgumentList, null, cancellationToken);
                }
            }
            else
            {
                switch (Node.Expression.Kind())
                {
                    case SyntaxKind.SimpleMemberAccessExpression:
                        EmitTree(((MemberAccessExpressionSyntax)Node.Expression).Expression, cancellationToken);
                        WriteDot();
                        Write(((MemberAccessExpressionSyntax)Node.Expression).Name.Identifier.ValueText);
                        break;
                    case SyntaxKind.ElementAccessExpression:
                        EmitTree(Node.Expression, cancellationToken);
                        WriteDot();
                        break;
                    case SyntaxKind.IdentifierName:
                        Write(((IdentifierNameSyntax)Node.Expression).Identifier.ValueText);
                        break;
                    default:
                        Debug.Fail("Unknown ezxpression for method invocation");
                        break;
                }
                WriteMethodInvocation(null, Node.ArgumentList, null, cancellationToken);
            }
        }

        private bool NeedsSpecialTypeExtension(IMethodSymbol methodSymbol)
        {
            switch (methodSymbol.ContainingType.SpecialType)
            {
                case SpecialType.None:
                case SpecialType.System_String:
                    return false;
                default:
                    return true;
            }
        }


        private ExpressionSyntax GetInvokeExpression(ExpressionSyntax e)
        {
            switch (e.Kind())
            {
                case SyntaxKind.MemberBindingExpression:
                    return ((MemberBindingExpressionSyntax)e).Name;
                case SyntaxKind.SimpleMemberAccessExpression:
                    return ((MemberAccessExpressionSyntax)e).Expression;
                default:
                    return e;
            }
        }
    }
}
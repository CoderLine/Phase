using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Phase.Translator.Utils;

namespace Phase.Translator.TypeScript.Expressions
{
    public class InvocationExpressionBlock : AutoCastBlockBase<InvocationExpressionSyntax>
    {
        public bool SkipSemicolonOnStatement { get; set; }

        protected override AutoCastMode DoEmitWithoutCast(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Node.Expression is IdentifierNameSyntax)
            {
                var value = Emitter.GetConstantValue(Node);
                if (value.HasValue)
                {
                    Write(value.Value);
                    return AutoCastMode.Default;
                }
            }

            var symbol = Emitter.GetSymbolInfo(Node.Expression).Symbol;
            var methodSymbol = symbol as IMethodSymbol;
            bool isEventInvocation = false;
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
                    case SymbolKind.Event:
                        if (((IEventSymbol)symbol).Type != null &&
                            ((IEventSymbol)symbol).Type.TypeKind == TypeKind.Delegate)
                        {
                            methodSymbol = ((INamedTypeSymbol)((IEventSymbol)symbol).Type).DelegateInvokeMethod;
                            isEventInvocation = true;
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

            var arguments = Node.ArgumentList.Arguments.Select(a => new ParameterInvocationInfo(a)).ToList();
            if (methodSymbol != null)
            {
                if (methodSymbol.IsExtensionMethod)
                {
                    methodSymbol = methodSymbol.ReducedFrom;
                    arguments.Insert(0, new ParameterInvocationInfo(GetInvokeExpression(Node.Expression)));
                }

                var template = Emitter.GetTemplate(methodSymbol);
                if (template != null)
                {
                    SkipSemicolonOnStatement = template.SkipSemicolonOnStatements;
                    if (template.Variables.TryGetValue("this", out var thisVar))
                    {
                        PushWriter();
                        if (methodSymbol.IsStatic)
                        {
                            Write(Emitter.GetTypeName(methodSymbol.ContainingType, false, true));
                        }
                        else
                        {
                            if (Node.Expression is MemberAccessExpressionSyntax memberAccess)
                            {
                                EmitTree(memberAccess.Expression, cancellationToken);
                            }
                            else
                            {
                                EmitTree(Node.Expression, cancellationToken);
                            }
                        }
                        thisVar.RawValue = PopWriter();
                    }

                    var methodInvocation = BuildMethodInvocation(methodSymbol, arguments);
                    ApplyExpressions(template,methodSymbol.Parameters, methodInvocation, methodSymbol.ContainingType, cancellationToken);
                    
                    for (int i = 0; i < methodSymbol.TypeArguments.Length; i++)
                    {
                        var argument = ((IMethodSymbol)symbol).TypeArguments[i];
                        var param = methodSymbol.TypeParameters[i];
                        if (template.Variables.TryGetValue(param.Name, out var variable))
                        {
                            variable.RawValue = Emitter.GetTypeName(argument);
                        }
                    }

                    Write(template.ToString());
                }
                else if (Emitter.IsMethodRedirected(methodSymbol, out var targetType))
                {
                    Write(targetType);
                    WriteDot();
                    Write(EmitterContext.GetMethodName(methodSymbol));
                    if (targetType.StartsWith("ph."))
                    {
                        EmitterContext.NeedsPhaseImport = true;
                    }
                    if (methodSymbol.IsStatic)
                    {
                        WriteMethodInvocation(methodSymbol, arguments, Node, cancellationToken);
                    }
                    else
                    {
                        arguments.Insert(0, new ParameterInvocationInfo(GetInvokeExpression(Node.Expression), true));
                        WriteMethodInvocation(methodSymbol, arguments, Node, cancellationToken);
                    }
                }
                else if (methodSymbol.IsStatic)
                {
                    Write(Emitter.GetTypeName(methodSymbol.ContainingType, false, true));
                    EmitterContext.ImportType(methodSymbol.ContainingType);
                    WriteDot();
                    Write(EmitterContext.GetMethodName(methodSymbol));
                    WriteMethodInvocation(methodSymbol, arguments, Node, cancellationToken);
                }
                else
                {
                    EmitTree(Node.Expression, cancellationToken);
                    if (isEventInvocation)
                    {
                        WriteDot();
                        Write(EmitterContext.GetMethodName(methodSymbol));
                    }
                    WriteMethodInvocation(methodSymbol, arguments, Node, cancellationToken);
                }
            }
            else
            {
                EmitTree(Node.Expression, cancellationToken);
                WriteMethodInvocation(null, arguments, Node, cancellationToken);

            }

            return AutoCastMode.Default;
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
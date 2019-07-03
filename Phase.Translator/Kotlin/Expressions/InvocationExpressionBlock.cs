using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin.Expressions
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
            bool isDelegateInvocation = false;
            if (methodSymbol == null && symbol != null)
            {
                switch (symbol.Kind)
                {
                    case SymbolKind.Parameter:
                        if (((IParameterSymbol)symbol).Type != null &&
                            ((IParameterSymbol)symbol).Type.TypeKind == TypeKind.Delegate)
                        {
                            methodSymbol = ((INamedTypeSymbol)((IParameterSymbol)symbol).Type).DelegateInvokeMethod;
                            isDelegateInvocation = true;
                        }
                        break;
                    case SymbolKind.Property:
                        if (((IPropertySymbol)symbol).Type != null &&
                            ((IPropertySymbol)symbol).Type.TypeKind == TypeKind.Delegate)
                        {
                            methodSymbol = ((INamedTypeSymbol)((IPropertySymbol)symbol).Type).DelegateInvokeMethod;
                            isDelegateInvocation = true;
                        }
                        break;
                    case SymbolKind.Field:
                        if (((IFieldSymbol)symbol).Type != null &&
                            ((IFieldSymbol)symbol).Type.TypeKind == TypeKind.Delegate)
                        {
                            methodSymbol = ((INamedTypeSymbol)((IFieldSymbol)symbol).Type).DelegateInvokeMethod;
                            isDelegateInvocation = true;
                        }
                        break;
                    case SymbolKind.Event:
                        if (((IEventSymbol)symbol).Type != null &&
                            ((IEventSymbol)symbol).Type.TypeKind == TypeKind.Delegate)
                        {
                            methodSymbol = ((INamedTypeSymbol)((IEventSymbol)symbol).Type).DelegateInvokeMethod;
                            isDelegateInvocation = true;
                        }
                        break;
                    case SymbolKind.Local:
                        if (((ILocalSymbol)symbol).Type != null &&
                            ((ILocalSymbol)symbol).Type.TypeKind == TypeKind.Delegate)
                        {
                            methodSymbol = ((INamedTypeSymbol)((ILocalSymbol)symbol).Type).DelegateInvokeMethod;
                            isDelegateInvocation = true;
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
                    foreach (var param in methodSymbol.Parameters)
                    {
                        if (template.Variables.TryGetValue(param.Name, out var variable))
                        {
                            var values = methodInvocation[param.Name].ToArray();
                            PushWriter();
                            if (param.IsParams)
                            {
                                if (values.Length == 1)
                                {
                                    var singleParamType = Emitter.GetTypeInfo(values[0]);
                                    if (singleParamType.ConvertedType.Equals(param.Type))
                                    {
                                        EmitTree(values[0], cancellationToken);
                                    }
                                    else
                                    {
                                        EmitTree(values[0], cancellationToken);
                                    }
                                }
                                else
                                {
                                    for (int j = 0; j < values.Length; j++)
                                    {
                                        if (j > 0) WriteComma();
                                        EmitTree(values[0], cancellationToken);
                                    }
                                }

                            }
                            else
                            {
                                if (variable.Modifier == "raw")
                                {
                                    var constValue = Emitter.GetConstantValue(values[0], cancellationToken);
                                    if (constValue.HasValue)
                                    {
                                        Write(constValue);
                                    }
                                    else
                                    {
                                        EmitTree(values[0], cancellationToken);
                                    }
                                }
                                else
                                {
                                    EmitTree(values[0], cancellationToken);
                                }
                            }
                            var paramOutput = PopWriter();
                            variable.RawValue = paramOutput;
                        }
                    }

                    for (int i = 0; i < methodSymbol.TypeArguments.Length; i++)
                    {
                        var argument = methodSymbol.TypeArguments[i];
                        var param = methodSymbol.TypeParameters[i];
                        if (template.Variables.TryGetValue(param.Name, out var variable))
                        {
                            variable.RawValue = Emitter.GetTypeName(argument, false, false);
                        }
                    }

                    Write(template.ToString());
                }
                else if (Emitter.IsMethodRedirected(methodSymbol, out var targetType))
                {
                    Write(targetType);
                    Write(".");
                    Write(Emitter.GetMethodName(methodSymbol));
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
                    Write(".");
                    Write(Emitter.GetMethodName(methodSymbol));
                    WriteMethodInvocation(methodSymbol, arguments, Node, cancellationToken);
                }
                else
                {
                    EmitTree(Node.Expression, cancellationToken);

                    if (isDelegateInvocation)
                    {
                        Write("!!");
                    }

                    WriteMethodInvocation(methodSymbol, arguments, Node, cancellationToken);

                    EmitterContext.WriteTypeParameterArrayCast(methodSymbol.OriginalDefinition.ReturnType, methodSymbol.ReturnType);

                    if (methodSymbol.OriginalDefinition.ReturnType.TypeKind == TypeKind.TypeParameter &&  methodSymbol.ReturnType.IsValueType)
                    {
                        Write("!!");
                    }
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
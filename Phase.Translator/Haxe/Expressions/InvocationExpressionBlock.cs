using System.Linq;
using System.Threading;
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
                            EmitTree(Node.Expression, cancellationToken);
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
                                for (int j = 0; j < values.Length; j++)
                                {
                                    if (j > 0) WriteComma();
                                    EmitTree(Node.Expression, cancellationToken);
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
                            variable.RawValue = Emitter.GetTypeName(argument);
                        }
                    }

                    Write(template.ToString());
                }
                else if (Emitter.IsMethodRedirected(methodSymbol, out var targetType))
                {
                    Write(targetType);
                    WriteDot();
                    Write(Emitter.GetMethodName(methodSymbol));
                    if (methodSymbol.IsStatic)
                    {
                        WriteMethodInvocation(methodSymbol, arguments, cancellationToken);
                    }
                    else
                    {
                        arguments.Insert(0, new ParameterInvocationInfo(GetInvokeExpression(Node.Expression), true));
                        WriteMethodInvocation(methodSymbol, arguments, cancellationToken);
                    }
                }
                else if (methodSymbol.IsStatic)
                {
                    Write(Emitter.GetTypeName(methodSymbol.ContainingType, false, true));
                    WriteDot();
                    Write(Emitter.GetMethodName(methodSymbol));
                    WriteMethodInvocation(methodSymbol, arguments, cancellationToken);
                }
                else
                {
                    EmitTree(Node.Expression, cancellationToken);
                    WriteMethodInvocation(methodSymbol, arguments, cancellationToken);
                }
            }
            else
            {
                EmitTree(Node.Expression, cancellationToken);
                WriteMethodInvocation(null, arguments, cancellationToken);
            }

            var typeInfo = Emitter.GetTypeInfo(Node, cancellationToken);
            // implicit cast
            if (typeInfo.ConvertedType != null && !typeInfo.Type.Equals(typeInfo.ConvertedType))
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
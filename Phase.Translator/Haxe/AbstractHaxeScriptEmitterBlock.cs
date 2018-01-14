using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public abstract class AbstractHaxeScriptEmitterBlock : AbstractEmitterBlock
    {
        public HaxeEmitterContext EmitterContext { get; set; }

        protected override IWriter Writer => EmitterContext.Writer;
        public HaxeEmitter Emitter => EmitterContext.Emitter;

        public void PushWriter()
        {
            EmitterContext.PushWriter();
        }

        public string PopWriter()
        {
            return EmitterContext.PopWriter();
        }

        protected AbstractHaxeScriptEmitterBlock(HaxeEmitterContext context)
        {
            EmitterContext = context;
        }

        protected AbstractHaxeScriptEmitterBlock EmitTree(SyntaxNode value, CancellationToken cancellationToken = default(CancellationToken))
        {
            var expressionBlock = new VisitorBlock(EmitterContext, value);
            expressionBlock.DoEmit(cancellationToken);
            return expressionBlock.FirstBlock;
        }


        protected void WriteType(TypeSyntax syntax)
        {
            WriteType(Emitter.GetTypeSymbol(syntax));
        }

        protected void WriteType(ITypeSymbol type)
        {
            Write(Emitter.GetTypeName(type));
        }

        protected void WriteAccessibility(Accessibility declaredAccessibility)
        {
            switch (declaredAccessibility)
            {
                case Accessibility.NotApplicable:
                case Accessibility.Private:
                case Accessibility.Protected:
                    Write("private ");
                    break;
                case Accessibility.ProtectedAndInternal:
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                case Accessibility.Public:
                    Write("public ");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        protected void WriteMethodInvocation(IMethodSymbol method,
            ArgumentListSyntax argumentList,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteMethodInvocation(method, argumentList?.Arguments.Select(a => new ParameterInvocationInfo(a)), cancellationToken);
        }

        protected void WriteMethodInvocation(IMethodSymbol method, IEnumerable<ParameterInvocationInfo> argumentList, CancellationToken cancellationToken = default(CancellationToken))
        {
            EmitterContext.IsMethodInvocation = true;
            WriteOpenParentheses();

            if (argumentList != null)
            {
                if (method == null)
                {
                    var args = argumentList.ToArray();
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (i > 0) WriteComma();
                        EmitTree(args[i].Expression, cancellationToken);
                    }
                }
                else
                {
                    BaseMethodDeclarationSyntax methodDeclaration = null;
                    foreach (var reference in method.DeclaringSyntaxReferences)
                    {
                        methodDeclaration = reference.GetSyntax(cancellationToken) as BaseMethodDeclarationSyntax;
                        if (methodDeclaration != null)
                        {
                            break;
                        }
                    }

                    var arguments = BuildMethodInvocation(method, argumentList);
                    var isFirstParam = true;
                    foreach (var argument in argumentList.Where(a => a.InjectAtBeginning))
                    {
                        if (!isFirstParam) WriteComma();
                        isFirstParam = false;
                        EmitTree(argument.Expression, cancellationToken);
                    }

                    // print expressions
                    for (int i = 0; i < method.Parameters.Length; i++)
                    {
                        if (!isFirstParam) WriteComma();
                        isFirstParam = false;

                        var param = method.Parameters[i];
                        var value = arguments[param.Name].ToArray();
                        if (param.IsParams)
                        {
                            Write("[");
                            for (var j = 0; j < value.Length; j++)
                            {
                                if (j > 0) WriteComma();
                                EmitTree(value[j], cancellationToken);
                            }

                            Write("]");
                        }
                        else
                        {
                            if (value.Length == 1)
                            {
                                EmitTree(value[0], cancellationToken);
                            }
                            else if (param.IsOptional)
                            {
                                if (methodDeclaration != null)
                                {
                                    var parameterDeclaration =
                                        methodDeclaration.ParameterList.Parameters[i].Default.Value;
                                    EmitTree(parameterDeclaration, cancellationToken);
                                }
                                else if (param.HasExplicitDefaultValue)
                                {
                                    Write(param.ExplicitDefaultValue);
                                }
                                else
                                {
                                    Write("null");
                                }
                            }
                        }
                    }
                }
            }

            WriteCloseParentheses();

            EmitterContext.IsMethodInvocation = false;
        }

        protected static Dictionary<string, IEnumerable<ExpressionSyntax>> BuildMethodInvocation(IMethodSymbol method, IEnumerable<ParameterInvocationInfo> argumentList)
        {
            var arguments = new Dictionary<string, IEnumerable<ExpressionSyntax>>();
            var varArgs = new List<ExpressionSyntax>();
            var varArgsName = string.Empty;
            // fill expected parameters
            foreach (var param in method.Parameters)
            {
                arguments[param.Name] = Enumerable.Empty<ExpressionSyntax>();
            }

            // iterate all actual parameters and fit the into the arguments lookup
            var parameterIndex = 0;
            var isVarArgs = false;
            foreach (var argument in argumentList)
            {
                if (argument.InjectAtBeginning)
                {
                    continue;
                }

                if (argument.Name != null && argument.Name != varArgsName)
                {
                    arguments[argument.Name] = new[] { argument.Expression };
                }

                if (isVarArgs)
                {
                    varArgs.Add(argument.Expression);
                }
                else if (parameterIndex < method.Parameters.Length)
                {
                    var param = method.Parameters[parameterIndex];
                    if (param.IsParams)
                    {
                        isVarArgs = true;
                        varArgsName = param.Name;
                        arguments[param.Name] = varArgs;
                        varArgs.Add(argument.Expression);
                    }
                    else
                    {
                        arguments[param.Name] = new[] { argument.Expression };
                        parameterIndex++;
                    }
                }
            }

            return arguments;
        }

        protected async Task WriteParameterDeclarations(ImmutableArray<IParameterSymbol> methodParameters, CancellationToken cancellationToken)
        {
            for (int i = 0; i < methodParameters.Length; i++)
            {
                if (i > 0)
                {
                    WriteComma();
                }

                var param = methodParameters[i];

                Write(param.Name);
                WriteSpace();
                WriteColon();
                if (param.RefKind != RefKind.None)
                {
                    throw new PhaseCompilerException("ref parameters are not supported");
                    Write("CsRef<");
                }
                WriteType(methodParameters[i].Type);
                if (param.RefKind != RefKind.None)
                {
                    Write(">");
                }

                if (param.IsOptional)
                {
                    Write(" = ");

                    var parameterSyntax = (ParameterSyntax)await methodParameters[i].DeclaringSyntaxReferences.First().GetSyntaxAsync(cancellationToken);
                    EmitTree(parameterSyntax.Default.Value, cancellationToken);
                }
            }
        }

    }

    public class ParameterInvocationInfo
    {
        public bool InjectAtBeginning { get; set; }
        public string Name { get; set; }
        public ExpressionSyntax Expression { get; set; }

        public ParameterInvocationInfo(ArgumentSyntax syntax)
        {
            if (syntax.NameColon != null)
            {
                Name = syntax.NameColon.Name.Identifier.ValueText;
            }
            Expression = syntax.Expression;
        }
        public ParameterInvocationInfo(ExpressionSyntax expression, bool injected = false)
        {
            Expression = expression;
            InjectAtBeginning = injected;
        }
    }

    public abstract class AbstractHaxeScriptEmitterBlock<T> : AbstractHaxeScriptEmitterBlock where T : SyntaxNode
    {
        public T Node { get; set; }

        protected AbstractHaxeScriptEmitterBlock() : base(null)
        {
        }

        public virtual void Emit(HaxeEmitterContext context, T node, CancellationToken cancellationToken = default(CancellationToken))
        {
            EmitterContext = context;
            Node = node;
            Emit(cancellationToken);
        }
    }
}
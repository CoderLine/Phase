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
        private HaxeEmitterContext _emitterContext;

        public HaxeEmitterContext EmitterContext
        {
            get => _emitterContext;
            set
            {
                _emitterContext = value;
                if (value != null)
                {
                    Writer = value.Writer;
                }
                else
                {
                    Writer = null;
                }
            }
        }

        public HaxeEmitter Emitter => EmitterContext.Emitter;

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


        protected void WriteMethodInvocation(IMethodSymbol method, ArgumentListSyntax argumentList, ExpressionSyntax extensionThis = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            EmitterContext.IsMethodInvocation = true;
            WriteOpenParentheses();
            if (extensionThis != null)
            {
                EmitTree(extensionThis, cancellationToken);
            }

            if (method == null)
            {
                for (int i = 0; i < argumentList.Arguments.Count; i++)
                {
                    if (i > 0 || extensionThis != null) WriteComma();
                    EmitTree(argumentList.Arguments[i], cancellationToken);
                }
            }
            else if (method.Parameters.Length > 0)
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

                var arguments = new Dictionary<string, ExpressionSyntax>();
                var varArgs = new List<ExpressionSyntax>();
                // fill expected parameters
                foreach (var param in method.Parameters)
                {
                    arguments[param.Name] = null;
                }

                // iterate all actual parameters and fit the into the arguments lookup
                var parameterIndex = 0;
                var isVarArgs = false;
                foreach (var argument in argumentList.Arguments)
                {
                    if (argument.NameColon != null)
                    {
                        arguments[argument.NameColon.Name.Identifier.Text] = argument.Expression;
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
                            varArgs.Add(argument.Expression);
                        }
                        else
                        {
                            arguments[param.Name] = argument.Expression;
                            parameterIndex++;
                        }
                    }
                }

                // print expressions
                for (int i = 0; i < method.Parameters.Length; i++)
                {
                    if (i > 0 || extensionThis != null) WriteComma();

                    var param = method.Parameters[i];
                    if (param.IsParams)
                    {
                        Write("[");
                        for (int j = 0; j < varArgs.Count; j++)
                        {
                            if (j > 0) WriteComma();
                            EmitTree(varArgs[j], cancellationToken);
                        }
                        Write("]");
                    }
                    else
                    {
                        var value = arguments[param.Name];
                        if (value != null)
                        {
                            EmitTree(value, cancellationToken);
                        }
                        else if (param.IsOptional)
                        {
                            if (methodDeclaration != null)
                            {
                                var parameterDeclaration = methodDeclaration.ParameterList.Parameters[i].Default.Value;
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

            WriteCloseParentheses();

            EmitterContext.IsMethodInvocation = false;
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
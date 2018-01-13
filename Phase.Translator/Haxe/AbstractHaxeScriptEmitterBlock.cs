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
        private HaxeEmitter _emitter;

        public new HaxeEmitter Emitter
        {
            get { return _emitter; }
            set
            {
                _emitter = value;
                base.Emitter = value;
            }
        }

        protected AbstractHaxeScriptEmitterBlock(HaxeEmitter emitter)
            :base(emitter)
        {
            Emitter = emitter;
        }

        protected async Task<AbstractHaxeScriptEmitterBlock> EmitTreeAsync(SyntaxNode value, CancellationToken cancellationToken = default(CancellationToken))
        {
            var expressionBlock = new VisitorBlock(Emitter, value);
            await expressionBlock.DoEmitAsync(cancellationToken);
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


        protected async Task WriteMethodInvocation(IMethodSymbol method, ArgumentListSyntax argumentList, ExpressionSyntax extensionThis = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Emitter.IsMethodInvocation = true;
            WriteOpenParentheses();
            if (extensionThis != null)
            {
                await EmitTreeAsync(extensionThis, cancellationToken);
            }

            if (method == null)
            {
                for (int i = 0; i < argumentList.Arguments.Count; i++)
                {
                    if (i > 0 || extensionThis != null) WriteComma();
                    await EmitTreeAsync(argumentList.Arguments[i], cancellationToken);
                }
            }
            else if (method.Parameters.Length > 0)
            {
                BaseMethodDeclarationSyntax methodDeclaration = null;
                foreach (var reference in method.DeclaringSyntaxReferences)
                {
                    methodDeclaration =
                        (await reference.GetSyntaxAsync(cancellationToken)) as BaseMethodDeclarationSyntax;
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
                            await EmitTreeAsync(varArgs[j], cancellationToken);
                        }
                        Write("]");
                    }
                    else
                    {
                        var value = arguments[param.Name];
                        if (value != null)
                        {
                            await EmitTreeAsync(value, cancellationToken);
                        }
                        else if (param.IsOptional)
                        {
                            if (methodDeclaration != null)
                            {
                                var parameterDeclaration = methodDeclaration.ParameterList.Parameters[i].Default.Value;
                                await EmitTreeAsync(parameterDeclaration, cancellationToken);
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

            Emitter.IsMethodInvocation = false;
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
                    await EmitTreeAsync(parameterSyntax.Default.Value, cancellationToken);
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

        public virtual async Task EmitAsync(HaxeEmitter emitter, T node, CancellationToken cancellationToken = default(CancellationToken))
        {
            Emitter = emitter;
            Node = node;
            await EmitAsync(cancellationToken);
        }
    }
}
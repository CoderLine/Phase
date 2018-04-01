using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator
{
    public abstract partial class AbstractEmitterBlock
    {
        public virtual void Emit(CancellationToken cancellationToken = default(CancellationToken))
        {
            BeginEmit(cancellationToken);
            DoEmit(cancellationToken);
            EndEmit(cancellationToken);
        }

        protected virtual void BeginEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
        }

        protected abstract void DoEmit(CancellationToken cancellationToken = default(CancellationToken));

        protected virtual void EndEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
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
                else if (isVarArgs)
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

}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Java
{
    public class FieldBlock : AbstractJavaEmitterBlock
    {
        private readonly IFieldSymbol _field;

        public FieldBlock(JavaEmitterContext context, IFieldSymbol field)
            : base(context)
        {
            _field = field;
        }

        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteComments(_field, cancellationToken);

            WriteAccessibility(_field.DeclaredAccessibility);
            if (_field.IsConst)
            {
                Write("static final ");
            }
            else if (_field.IsStatic)
            {
                Write("static ");
            }

            WriteType(_field.Type);

            var fieldName = Emitter.GetFieldName(_field);
            Write(" ", fieldName, " ");

            EmitterContext.IsConstInitializer = _field.IsConst;

            ExpressionSyntax initializer = null;
            foreach (var reference in _field.DeclaringSyntaxReferences)
            {
                var node = reference.GetSyntax(cancellationToken);
                var variable = node as VariableDeclaratorSyntax;
                if (variable != null)
                {
                    initializer = variable.Initializer?.Value;
                    if (initializer != null)
                    {
                        break;
                    }
                }
            }

            if (initializer != null)
            {
                Write(" = ");
                EmitTree(initializer, cancellationToken);
            }

            EmitterContext.IsConstInitializer = false;

            WriteSemiColon(true);

            WriteComments(_field, false, cancellationToken);
        }
    }
}
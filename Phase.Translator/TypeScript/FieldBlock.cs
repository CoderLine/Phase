using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript
{
    public class FieldBlock : AbstractTypeScriptEmitterBlock
    {
        private readonly IFieldSymbol _field;

        public FieldBlock(TypeScriptEmitterContext context, IFieldSymbol field)
            : base(context)
        {
            _field = field;
        }

        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_field.AssociatedSymbol is IPropertySymbol property && Emitter.IsAutoProperty(property))
            {
                return;
            }

            WriteComments(_field, cancellationToken);
            WriteMeta(_field, cancellationToken);

            WriteAccessibility(_field.DeclaredAccessibility);
            if (_field.IsConst)
            {
                Write("static readonly ");
            }
            else if (_field.IsStatic)
            {
                Write("static ");
            }

            var fieldName = Emitter.GetFieldName(_field);
            Write(fieldName, " ");

            if (_field.IsConst)
            {
                //Write("(default, never)");
            }

            WriteColon();
            Write(Emitter.GetTypeNameWithNullability(_field.Type));
            EmitterContext.ImportType(_field.Type);

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
            else if (_field.IsStatic)
            {
                Write(" = ");
                Write(Emitter.GetDefaultValue(_field.Type));
            }

            EmitterContext.IsConstInitializer = false;

            WriteSemiColon(true);

            WriteComments(_field, false, cancellationToken);
        }
    }
}
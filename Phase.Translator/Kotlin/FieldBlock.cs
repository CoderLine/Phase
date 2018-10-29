using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin
{
    public class FieldBlock : AbstractKotlinEmitterBlock
    {
        private readonly IFieldSymbol _field;

        public FieldBlock(KotlinEmitterContext context, IFieldSymbol field)
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

            if (_field.IsStatic)
            {
                Write("@JvmStatic");
                WriteNewLine();
            }

            WriteMeta(_field, cancellationToken);
            WriteAccessibility(_field.DeclaredAccessibility);

            
            if (_field.IsConst)
            {
                Write("val "); // kotlin const is not properly compatible with @JvmStatic
            }
            else
            {
                Write("var ");
            }

            var fieldName = Emitter.GetFieldName(_field);
            Write(" ", fieldName, " : ");
            Write(Emitter.GetTypeName(_field.Type, false, false, !_field.IsConst));
      
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

            Write(" = ");
            if (initializer != null)
            {
                EmitTree(initializer, cancellationToken);
            }
            else
            {
                Write(Emitter.GetDefaultValue(_field.Type));
            }

            EmitterContext.IsConstInitializer = false;

            WriteNewLine();

            WriteComments(_field, false, cancellationToken);
        }
    }
}
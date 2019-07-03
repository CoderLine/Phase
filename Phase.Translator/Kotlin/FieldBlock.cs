using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
            var property = _field.AssociatedSymbol as IPropertySymbol;
            if (property != null && Emitter.IsAutoProperty(property))
            {
                return;
            }

            var declaration = _field.DeclaringSyntaxReferences
                .Select(r => r.GetSyntax(cancellationToken))
                .OfType<VariableDeclaratorSyntax>()
                .FirstOrDefault();

            
            WriteComments(_field, cancellationToken);

            if (_field.IsStatic)
            {
                Write("@JvmStatic");
                WriteNewLine();
            }

            WriteMeta(_field, cancellationToken);
            WriteAccessibility(_field.DeclaredAccessibility);

            bool lateInit = false;
            if (declaration?.Initializer != null)
            {
                if (declaration.Initializer.Value.Kind() == SyntaxKind.SuppressNullableWarningExpression)
                {
                    switch (((PostfixUnaryExpressionSyntax) declaration.Initializer.Value).Operand.Kind())
                    {
                        case SyntaxKind.NullLiteralExpression:
                        case SyntaxKind.DefaultLiteralExpression:
                            Write("lateinit ");
                            lateInit = true;
                            break;
                    }
                }
            }


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
            Write(Emitter.GetTypeName(_field.Type, false, false,
                _field.NullableAnnotation == NullableAnnotation.Annotated));

            EmitterContext.IsConstInitializer = _field.IsConst;

            if (!lateInit)
            {
                if (declaration != null && declaration.Initializer != null)
                {
                    Write(" = ");
                    EmitTree(declaration.Initializer, cancellationToken);
                }
                else if (property != null)
                {
                    var propertyInitializer = property.DeclaringSyntaxReferences
                        .Select(r => r.GetSyntax(cancellationToken))
                        .OfType<PropertyDeclarationSyntax>()
                        .Select(p => p.Initializer)
                        .FirstOrDefault(p => p != null);
                    if (propertyInitializer != null)
                    {
                        Write(" = ");
                        EmitTree(propertyInitializer, cancellationToken);
                    }
                    else
                    {
                        var defaultValue = Emitter.GetDefaultValue(_field.Type);
                        if (defaultValue != "null")
                        {
                            Write(" = ", defaultValue);
                        }
                    }
                }
                else
                {
                    var defaultValue = Emitter.GetDefaultValue(_field.Type);
                    if (defaultValue != "null")
                    {
                        Write(" = ", defaultValue);
                    }
                }
            }

            EmitterContext.IsConstInitializer = false;

            WriteNewLine();

            WriteComments(_field, false, cancellationToken);
        }
    }
}
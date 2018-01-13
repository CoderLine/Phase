using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Haxe
{
    public class FieldBlock : AbstractHaxeScriptEmitterBlock
    {
        private readonly IFieldSymbol _field;

        public FieldBlock(HaxeEmitterContext context, IFieldSymbol field)
            : base(context)
        {
            _field = field;
        }

        protected override async Task DoEmitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_field.AssociatedSymbol is IPropertySymbol property && Emitter.IsAutoProperty(property))
            {
                return;
            }

            WriteAccessibility(_field.DeclaredAccessibility);
            if (_field.IsConst)
            {
                Write("static inline ");
            }
            else if (_field.IsStatic)
            {
                Write("static ");
            }

            var fieldName = Emitter.GetFieldName(_field);
            Write("var ", fieldName, " ");

            if (_field.IsConst)
            {
                //Write("(default, never)");
            }

            WriteColon();
            WriteType(_field.Type);

            ExpressionSyntax initializer = null;
            foreach (var reference in _field.DeclaringSyntaxReferences)
            {
                var node = await reference.GetSyntaxAsync(cancellationToken);
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
                await EmitTreeAsync(initializer, cancellationToken);
            }

            WriteSemiColon(true);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Cpp
{
    class FieldSourceBlock : AbstractCppEmitterBlock
    {
        private readonly IFieldSymbol _field;

        public FieldSourceBlock(CppEmitterContext emitterContext, IFieldSymbol field)
        {
            _field = field;
            Init(emitterContext);
        }

        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            EmitterContext.ImportType(_field.Type);

            if (!_field.IsStatic)
            {
                return;
            }

            var fieldName = Emitter.GetFieldName(_field);
            EmitterContext.ImportType(_field.Type);

            Write(Emitter.GetTypeName(_field.Type, false, false, CppEmitter.TypeNamePointerKind.SharedPointerDeclaration));

            var typeName = Emitter.GetTypeName(_field.ContainingType, false, true, CppEmitter.TypeNamePointerKind.NoPointer);
            Write(" ", typeName);
            Write("::");
            Write(fieldName);

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
            WriteSemiColon(true);
        }
    }
}
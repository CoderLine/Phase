using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Cpp
{
    class FieldHeaderBlock : AbstractCppEmitterBlock
    {
        private readonly IFieldSymbol _field;

        public FieldHeaderBlock(CppEmitterContext emitterContext, IFieldSymbol field)
        {
            _field = field;
            Init(emitterContext);
        }

        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteComments(_field, cancellationToken);

            WriteAccessibility(_field.DeclaredAccessibility);
            if (_field.IsStatic)
            {
                Write("static ");
            }

            var fieldName = Emitter.GetFieldName(_field);
            EmitterContext.ImportType(_field.Type);

            Write(Emitter.GetTypeName(_field.Type, false, false, CppEmitter.TypeNamePointerKind.SharedPointerDeclaration));
            WriteSpace();
            Write(fieldName);
            WriteSemiColon(true);
        }
    }
}
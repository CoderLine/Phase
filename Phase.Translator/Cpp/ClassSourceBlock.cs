using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Cpp
{
    class ClassSourceBlock : AbstractCppEmitterBlock
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            var type = (PhaseClass)EmitterContext.CurrentType;
            if (Emitter.IsExternal(type.TypeSymbol))
            {
                return;
            }

            Write("#include \"stdafx.h\"");
            WriteNewLine();
            WriteDefaultFileHeader();
            WriteInclude(EmitterContext.CurrentType.TypeSymbol);

            PushWriter();

            EmitClass(cancellationToken);

            var result = PopWriter();

            foreach (var importedType in EmitterContext.ImportedTypes.Values)
            {
                WriteInclude(importedType.Type);
            }
            WriteNewLine();

            Write(result);
        }

        private void EmitClass(CancellationToken cancellationToken)
        {
            var type = (PhaseClass)EmitterContext.CurrentType;
            if (Emitter.IsExternal(type.TypeSymbol))
            {
                return;
            }

            var fullName = Emitter.GetTypeName(type.TypeSymbol, false, true, CppEmitter.TypeNamePointerKind.NoPointer);
            var parts = fullName.Split(new[] { "::" }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 1)
            {
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    Write("namespace ", parts[i], "{");
                }
            }

            WriteNewLine();
            Indent();

            foreach (var member in type.TypeSymbol.GetMembers().OrderByDescending(m => m.DeclaredAccessibility))
            {
                EmitterContext.CurrentMember = member;
                switch (member.Kind)
                {
                    case SymbolKind.Field:
                        var fieldBlock = new FieldSourceBlock(EmitterContext, (IFieldSymbol)member);
                        fieldBlock.Emit(cancellationToken);
                        break;
                    case SymbolKind.Method:
                        var methodBlock = new MethodSourceBlock(EmitterContext, (IMethodSymbol)member);
                        methodBlock.Emit(cancellationToken);
                        break;
                    case SymbolKind.Event:
                        //var eventBlock = new EventHeaderBlock(EmitterContext, (IEventSymbol)member);
                        //eventBlock.Emit(cancellationToken);
                        break;
                }
            }

            if (parts.Length > 1)
            {
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    Write("}");
                }
            }
        }
    }
}
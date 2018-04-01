using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Cpp
{
    class EnumHeaderBlock : AbstractCppEmitterBlock
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            Write("#pragma once");
            WriteNewLine();

            WriteDefaultFileHeader();

            PushWriter();

            EmitClass(cancellationToken);

            var result = PopWriter();

            // forward declarations
            foreach (var importedType in EmitterContext.ImportedTypes.Values)
            {
                if (importedType.RequiresInclude)
                {
                    WriteInclude(importedType.Type);
                }
                else
                {
                    WriteForwardDeclaration(importedType.Type);
                }
            }
            WriteNewLine();

            Write(result);
        }

        private void EmitClass(CancellationToken cancellationToken)
        {
            var type = (PhaseEnum)EmitterContext.CurrentType;

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
                    Write("namespace ", parts[i], " {");
                }
            }

            WriteNewLine();
            Indent();

            EmitterContext.ImportType(Emitter.GetSpecialType(SpecialType.System_String));

            WriteComments(type.TypeSymbol, cancellationToken);
            var name = parts.Last();
            Write("enum class ");
            WriteDeclspec();
            Write(" ", name, " : ");
            WriteType(type.TypeSymbol.EnumUnderlyingType);
            EmitterContext.ImportType(type.TypeSymbol.EnumUnderlyingType);
            WriteNewLine();

            BeginBlock();

            var isFirst = true;
            foreach (var enumMember in type.TypeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                if (!isFirst)
                {
                    Write(",");
                    WriteNewLine();
                }
                isFirst = false;
                var enumMemberBlock = new EnumMemberBlock(EmitterContext, enumMember);
                enumMemberBlock.Emit(cancellationToken);
            }

            WriteNewLine();
            EndBlock(false);
            WriteSemiColon(true);

            WriteComments(type.TypeSymbol, false, cancellationToken);


            Write("PHASE_DEFINE_ENUM_OPERATORS(", name, ", ");
            WriteType(type.TypeSymbol.EnumUnderlyingType);
            Write(")");
            WriteNewLine();

            Outdent();
            if (parts.Length > 1)
            {
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    Write("}");
                }
            }

            WriteComments(type.RootNode.SyntaxTree.GetRoot(cancellationToken), false);

        }
    }
}
using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Cpp
{
    class ClassHeaderBlock : AbstractCppEmitterBlock
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
            var type = EmitterContext.CurrentType;
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

            var name = parts.Last();

            if (type.TypeSymbol.IsGenericType)
            {
                Write("template <");

                var typeParameters = type.TypeSymbol.TypeParameters;
                var t = type.TypeSymbol;
                while (typeParameters.Length == 0 && t.ContainingType != null)
                {
                    typeParameters = t.ContainingType.TypeParameters;
                    t = t.ContainingType;
                }

                for (int i = 0; i < typeParameters.Length; i++)
                {
                    if (i > 0) Write(", ");
                    Write("typename ", typeParameters[i].Name);
                }

                Write(">");
                WriteNewLine();
            }

            Write("class ");
            WriteDeclspec();
            Write(" ", name);

            Writer.Indent();

            var baseTypes = type.TypeSymbol.BaseType != null
                ? new[] { type.TypeSymbol.BaseType }.Concat(type.TypeSymbol.Interfaces)
                : type.TypeSymbol.Interfaces;

            var isFirst = true;
            foreach (var baseType in baseTypes)
            {
                WriteNewLine();
                if (isFirst) Write(": ");
                else Write(", ");
                isFirst = false;

                Write("public ");

                if (baseType.TypeKind == TypeKind.Interface)
                {
                    Write("virtual ");
                }

                EmitterContext.ImportType(baseType, true);
                Write(Emitter.GetTypeName(baseType, pointerKind: CppEmitter.TypeNamePointerKind.NoPointer));
            }

            Writer.Outdent();

            WriteNewLine();

            BeginBlock();

            var hasConstructor = false;
            foreach (var member in type.TypeSymbol.GetMembers().OrderByDescending(m => m.DeclaredAccessibility))
            {
                EmitterContext.CurrentMember = member;
                switch (member.Kind)
                {
                    case SymbolKind.Field:
                        var fieldBlock = new FieldHeaderBlock(EmitterContext, (IFieldSymbol)member);
                        fieldBlock.Emit(cancellationToken);
                        break;
                    case SymbolKind.Method:
                        var methodBlock = new MethodHeaderBlock(EmitterContext, (IMethodSymbol)member);
                        methodBlock.Emit(cancellationToken);

                        if (((IMethodSymbol)member).MethodKind == MethodKind.Constructor)
                        {
                            hasConstructor = true;
                        }

                        break;
                    case SymbolKind.Event:
                        var eventBlock = new EventHeaderBlock(EmitterContext, (IEventSymbol)member);
                        eventBlock.Emit(cancellationToken);
                        break;
                }
            }

            WriteAccessibility("public");

            // Emit default constructor
            if(type.Kind != PhaseTypeKind.Interface && !hasConstructor)
            {
                Write("", name, "() = default");
                WriteSemiColon(true);
                WriteNewLine();
            }

            Write("virtual ~", name, "() = default");
            WriteSemiColon(true);
            WriteNewLine();

            EndBlock(false);
            WriteSemiColon(true);

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

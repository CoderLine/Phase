using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.TypeScript
{
    public static class PhaseConstants
    {
        public const string Phase = "Phase";
        public const string ConstructorPrefix = "__Init";
    }

    public class ClassBlock : AbstractTypeScriptEmitterBlock
    {
        private readonly PhaseType _type;

        public ClassBlock(TypeScriptEmitterContext emitter)
            : base(emitter)
        {
            _type = emitter.CurrentType;
        }

        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Emitter.IsExternal(_type.TypeSymbol))
            {
                return;
            }

            PushWriter();
            EmitClass(cancellationToken);

            var result = PopWriter();
            
            Write("import * as phase from '@mscorlib/phase'");
            WriteNewLine();

            foreach (var importedType in EmitterContext.ImportedTypes.Values)
            {
                WriteImport(importedType.Type);
            }

            WriteNewLine();

            Write(result);
        }
        
        protected void EmitClass(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Emitter.IsExternal(_type.TypeSymbol))
            {
                return;
            }

            var fullName = Emitter.GetTypeName(_type.TypeSymbol, noTypeArguments: true);
            var packageEnd = fullName.LastIndexOf(".", StringComparison.Ordinal);

            var name = packageEnd == -1 ? fullName : fullName.Substring(packageEnd + 1);

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken));

            WriteComments(_type.TypeSymbol, cancellationToken);

            WriteMeta(_type.TypeSymbol, cancellationToken);

            Write("export class ", name);

            if (_type.TypeSymbol.IsGenericType)
            {
                var typeParameters = _type.TypeSymbol.TypeParameters;
                var t = _type.TypeSymbol;
                while (typeParameters.Length == 0 && t.ContainingType != null)
                {
                    typeParameters = t.ContainingType.TypeParameters;
                    t = t.ContainingType;
                }

                Write("<");
                for (int i = 0; i < typeParameters.Length; i++)
                {
                    if (i > 0) Write(", ");
                    Write(typeParameters[i].Name);
                }

                Write(">");
            }

            if (!_type.TypeSymbol.IsStatic)
            {
                if (_type.TypeSymbol.BaseType != null &&
                    _type.TypeSymbol.BaseType.SpecialType != SpecialType.System_Object)
                {
                    Write(" extends ");
                    WriteType(_type.TypeSymbol.BaseType);
                    EmitterContext.ImportType(_type.TypeSymbol.BaseType);
                }

                foreach (var type in _type.TypeSymbol.Interfaces)
                {
                    Write(" implements ");
                    WriteType(type);
                    EmitterContext.ImportType(type);
                }
            }

            WriteNewLine();
            BeginBlock();

            foreach (var member in _type.TypeSymbol.GetMembers())
            {
                EmitterContext.CurrentMember = member;
                if (member.Kind == SymbolKind.Field)
                {
                    var fieldBlock = new FieldBlock(EmitterContext, (IFieldSymbol) member);
                    fieldBlock.Emit(cancellationToken);
                }
                else if (member.Kind == SymbolKind.Property)
                {
                    var propertyBlock = new PropertyBlock(EmitterContext, (IPropertySymbol) member);
                    propertyBlock.Emit(cancellationToken);
                }
                else if (member.Kind == SymbolKind.Method)
                {
                    var methodBlock = new MethodBlock(EmitterContext, (IMethodSymbol) member);
                    methodBlock.Emit(cancellationToken);
                }
                else if (member.Kind == SymbolKind.Event)
                {
                    var eventBlock = new EventBlock(EmitterContext, (IEventSymbol) member);
                    eventBlock.Emit(cancellationToken);
                }
            }

            // Emit default constructor
            if (!Emitter.HasNativeConstructors(_type.TypeSymbol) && (Emitter.HasConstructorOverloads(_type.TypeSymbol)))
            {
                WriteAccessibility(Accessibility.Public);
                Write("constructor");
                WriteOpenCloseParentheses();
                WriteNewLine();
                BeginBlock();
                if (_type.TypeSymbol.BaseType != null &&
                    _type.TypeSymbol.BaseType.SpecialType != SpecialType.System_Object &&
                    !Emitter.IsAbstract(_type.TypeSymbol))
                {
                    Write("super();");
                    WriteNewLine();
                }

                WriteDefaultInitializers(_type.TypeSymbol, false, cancellationToken);

                EndBlock();
            }

            EndBlock();

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken), false);
        }
    }
}
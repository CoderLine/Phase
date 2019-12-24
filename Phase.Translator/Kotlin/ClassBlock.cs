﻿using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Kotlin
{
    public class ClassBlock : AbstractKotlinEmitterBlock
    {
        private readonly PhaseType _type;
        private string _package;
        private string _name;

        public ClassBlock(KotlinEmitterContext emitter)
            : this(emitter.CurrentType, emitter)
        {
        }

        public ClassBlock(PhaseType type, KotlinEmitterContext emitter)
            : base(emitter)
        {
            _type = type;
            var fullName = Emitter.GetTypeName(_type.TypeSymbol, false, true, false);
            var packageEnd = fullName.LastIndexOf(".", StringComparison.Ordinal);
            if (packageEnd == -1)
            {
                _package = "";
                _name = fullName;
            }
            else
            {
                _package = fullName.Substring(0, packageEnd);
                _name = fullName.Substring(packageEnd + 1);
            }
        }

        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Emitter.IsExternal(_type.TypeSymbol))
            {
                return;
            }

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken));

            if (_package.Length > 1)
            {
                Write("package ");
                Write(_package);
                WriteNewLine();
            }

            EmitNested(cancellationToken);
        }

        public void EmitNested(CancellationToken cancellationToken)
        {
            WriteComments(_type.TypeSymbol, cancellationToken);

            WriteAccessibility(_type.TypeSymbol.DeclaredAccessibility);

            if (_type.TypeSymbol.IsAbstract)
            {
                Write("abstract ");
            }
            else
            {
                Write("open ");
            }


            Write("class ", _name);

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
                var colonWritten = false;
                if (_type.TypeSymbol.BaseType != null &&
                    _type.TypeSymbol.BaseType.SpecialType != SpecialType.System_Object)
                {
                    Write(" : ");
                    colonWritten = true;
                    Write(Emitter.GetTypeName(_type.TypeSymbol.BaseType, false, false, false));
                }

                if (_type.TypeSymbol.Interfaces.Length > 0)
                {
                    if (!colonWritten) Write(" : ");
                    for (int i = 0; i < _type.TypeSymbol.Interfaces.Length; i++)
                    {
                        if (i > 0) WriteComma();
                        INamedTypeSymbol type = _type.TypeSymbol.Interfaces[i];
                        Write(Emitter.GetTypeName(type, false, false, false));
                    }
                }
            }

            WriteNewLine();
            BeginBlock();

            foreach (var group in _type.TypeSymbol.GetMembers().GroupBy(m => m.IsStatic))
            {
                if (group.Key)
                {
                    Write("companion object");
                    BeginBlock();
                }

                foreach (var member in group)
                {
                    EmitterContext.CurrentMember = member;
                    switch (member.Kind)
                    {
                        case SymbolKind.Event:
                            var eventBlock = new EventBlock(EmitterContext, (IEventSymbol)member);
                            eventBlock.Emit(cancellationToken);
                            break;
                        case SymbolKind.Field:
                            var fieldBlock = new FieldBlock(EmitterContext, (IFieldSymbol)member);
                            fieldBlock.Emit(cancellationToken);
                            break;
                        case SymbolKind.Method:
                            var methodBlock = new MethodBlock(EmitterContext, (IMethodSymbol)member);
                            methodBlock.Emit(cancellationToken);
                            break;
                        case SymbolKind.Property:
                            var propertyBlock = new PropertyBlock(EmitterContext, (IPropertySymbol)member);
                            propertyBlock.Emit(cancellationToken);
                            break;
                        case SymbolKind.NamedType:
                            EmitterContext.WriteNestedType((INamedTypeSymbol)member, cancellationToken);
                            break;
                    }
                }


                if (group.Key)
                {
                    EndBlock();
                }
            }

            EndBlock();

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken), false);
        }
    }
}
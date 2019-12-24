﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.TypeScript
{
    public class InterfaceBlock : AbstractTypeScriptEmitterBlock
    {
        private readonly PhaseInterface _type;

        public InterfaceBlock(TypeScriptEmitterContext context)
            : base(context)
        {
            _type = (PhaseInterface)context.CurrentType;
        }

        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Emitter.IsExternal(_type.TypeSymbol))
            {
                return;
            }
            var fullName = Emitter.GetTypeName(_type.TypeSymbol, noTypeArguments: true);
            var index = fullName.LastIndexOf('.');

            var package = index >= 0 ? fullName.Substring(0, index) : null;
            var name = index >= 0 ? fullName.Substring(index + 1) : fullName;

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken));

            if (!string.IsNullOrEmpty(package))
            {
                Write("package ");
                Write(package);
                WriteSemiColon(true);
                WriteNewLine();
            }

            WriteComments(_type.TypeSymbol, cancellationToken);

            if (_type.TypeSymbol.DeclaredAccessibility == Accessibility.Public)
            {
                WriteNewLine();
            }

            Write("interface ", name);

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

            foreach (var type in _type.TypeSymbol.Interfaces)
            {
                Write(" extends ");
                WriteType(type);
            }

            WriteNewLine();
            BeginBlock();

            foreach (var member in _type.TypeSymbol.GetMembers())
            {
                switch (member.Kind)
                {
                    case SymbolKind.Field:
                        var fieldBlock = new FieldBlock(EmitterContext, (IFieldSymbol)member);
                        fieldBlock.Emit(cancellationToken);
                        break;
                    case SymbolKind.Property:
                        var propertyBlock = new PropertyBlock(EmitterContext, (IPropertySymbol)member);
                        propertyBlock.Emit(cancellationToken);
                        break;
                    case SymbolKind.Method:
                        var methodBlock = new MethodBlock(EmitterContext, (IMethodSymbol)member);
                        methodBlock.Emit(cancellationToken);
                        break;
                    case SymbolKind.Event:
                        var eventBlock = new EventBlock(EmitterContext, (IEventSymbol)member);
                        eventBlock.Emit(cancellationToken);
                        break;
                }
            }

            EndBlock();

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken), false);
        }
    }
}
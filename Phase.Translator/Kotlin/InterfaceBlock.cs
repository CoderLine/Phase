using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Kotlin
{
    public class InterfaceBlock : AbstractKotlinEmitterBlock
    {
        private readonly PhaseInterface _type;
        private string _package;
        private string _name;

        public InterfaceBlock(KotlinEmitterContext context)
            : this(context.CurrentType, context)
        {
        }

        public InterfaceBlock(PhaseType type, KotlinEmitterContext emitter)
            : base(emitter)
        {
            _type = (PhaseInterface)type;
            var fullName = Emitter.GetTypeName(_type.TypeSymbol, false, true);
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
                WriteSemiColon(true);
                WriteNewLine();
            }

            Write("import phase.extensions.*;");
            WriteNewLine();

            EmitNested(cancellationToken);
        }

        public void EmitNested(CancellationToken cancellationToken)
        {
            WriteComments(_type.TypeSymbol, cancellationToken);
            WriteMeta(_type.TypeSymbol, cancellationToken);

            WriteAccessibility(_type.TypeSymbol.DeclaredAccessibility);
            Write("interface ", _name);

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

            if (_type.TypeSymbol.Interfaces.Length > 0)
            {
                Write(" : ");

                for (int i = 0; i < _type.TypeSymbol.Interfaces.Length; i++)
                {
                    if (i > 0) WriteComma();
                    INamedTypeSymbol type = _type.TypeSymbol.Interfaces[i];
                    Write(Emitter.GetTypeName(type, false, false));
                }
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
                    case SymbolKind.Method:
                        var methodBlock = new MethodBlock(EmitterContext, (IMethodSymbol)member);
                        methodBlock.Emit(cancellationToken);
                        break;
                    case SymbolKind.Property:
                        var propertyBlock = new PropertyBlock(EmitterContext, (IPropertySymbol)member);
                        propertyBlock.Emit(cancellationToken);
                        break;
                }
            }

            EndBlock();

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken), false);
        }
    }
}
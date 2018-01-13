using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Haxe
{
    public class InterfaceBlock : AbstractHaxeScriptEmitterBlock
    {
        private readonly PhaseInterface _type;

        public InterfaceBlock(HaxeEmitterContext context, PhaseInterface type)
            : base(context)
        {
            _type = type;
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

            if (!string.IsNullOrEmpty(package))
            {
                Write("package ");
                Write(package);
                WriteSemiColon(true);
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

        }
    }
}
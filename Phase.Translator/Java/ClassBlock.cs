using System;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Java
{
    public class ClassBlock : AbstractJavaEmitterBlock
    {
        private readonly PhaseType _type;
        private string _package;
        private string _name;

        public ClassBlock(JavaEmitterContext emitter)
            : this(emitter.CurrentType, emitter)
        {
        }

        public ClassBlock(PhaseType type, JavaEmitterContext emitter)
            : base(emitter)
        {
            _type = type;
            var fullName = Emitter.GetTypeName(_type.TypeSymbol, noTypeArguments: true);
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

            EmitNested(cancellationToken);
        }

        public void EmitNested(CancellationToken cancellationToken)
        {
            WriteComments(_type.TypeSymbol, cancellationToken);

            WriteAccessibility(_type.TypeSymbol.DeclaredAccessibility);

            if (_type.TypeSymbol.ContainingType != null)
            {
                Write("static ");
            }
            if (_type.TypeSymbol.IsAbstract)
            {
                Write("abstract ");
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
                if (_type.TypeSymbol.BaseType != null && _type.TypeSymbol.BaseType.SpecialType != SpecialType.System_Object)
                {
                    Write(" extends ");
                    WriteType(_type.TypeSymbol.BaseType);
                }

                if (_type.TypeSymbol.Interfaces.Length > 0)
                {
                    Write(" implements ");
                    for (int i = 0; i < _type.TypeSymbol.Interfaces.Length; i++)
                    {
                        if (i > 0) WriteComma();
                        INamedTypeSymbol type = _type.TypeSymbol.Interfaces[i];
                        WriteType(type);
                    }
                }
            }

            WriteNewLine();
            BeginBlock();

            foreach (var member in _type.TypeSymbol.GetMembers())
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
                    case SymbolKind.NamedType:
                        EmitterContext.WriteNestedType((INamedTypeSymbol)member, cancellationToken);
                        break;
                }
            }

            EndBlock();

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken), false);
        }
    }
}
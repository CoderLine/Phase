using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Haxe
{
    public static class PhaseConstants
    {
        public const string Phase = "Phase";
        public const string PhaseDot = Phase + ".";
        public const string ConstructorPrefix = "__Init";

    }

    public class ClassBlock : AbstractHaxeScriptEmitterBlock
    {
        private readonly PhaseType _type;

        public ClassBlock(HaxeEmitterContext emitter, PhaseType type)
            : base(emitter)
        {
            _type = type;
        }

        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Emitter.IsExternal(_type.TypeSymbol))
            {
                return;
            }
            var abstractType = Emitter.GetAbstract(_type.TypeSymbol);

            var fullName = Emitter.GetTypeName(_type.TypeSymbol, noTypeArguments: true);
            var packageEnd = fullName.LastIndexOf(".", StringComparison.Ordinal);
            string package;
            string name;

            if (packageEnd == -1)
            {
                package = "";
                name = fullName;
            }
            else
            {
                package = fullName.Substring(0, packageEnd);
                name = fullName.Substring(packageEnd + 1);
            }

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken));

            if (package.Length > 1)
            {
                Write("package ");
                Write(package);
                WriteSemiColon(true);
                WriteNewLine();
            }

            WriteComments(_type.TypeSymbol, cancellationToken);

            if (abstractType != null)
            {
                Write("abstract ", name);
            }
            else
            {
                Write("class ", name);
            }

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

            if (abstractType != null)
            {
                WriteOpenParentheses();
                Write(abstractType.ConstructorArguments[0].Value);
                WriteCloseParentheses();

                if (abstractType.ConstructorArguments.Length == 3)
                {
                    Write(" from ");
                    Write(abstractType.ConstructorArguments[1].Value);
                    Write(" to ");
                    Write(abstractType.ConstructorArguments[2].Value);
                }
            }
            else if (!_type.TypeSymbol.IsStatic)
            {
                if (_type.TypeSymbol.BaseType != null && _type.TypeSymbol.BaseType.SpecialType != SpecialType.System_Object)
                {
                    Write(" extends ");
                    WriteType(_type.TypeSymbol.BaseType);
                }

                foreach (var type in _type.TypeSymbol.Interfaces)
                {
                    Write(" implements ");
                    WriteType(type);
                }
            }

            WriteNewLine();
            BeginBlock();

            foreach (var member in _type.TypeSymbol.GetMembers())
            {
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
                WriteFunction();
                Write("new");
                WriteOpenCloseParentheses();
                WriteNewLine();
                BeginBlock();
                if (_type.TypeSymbol.BaseType != null && _type.TypeSymbol.BaseType.SpecialType != SpecialType.System_Object &&
                    !Emitter.IsAbstract(_type.TypeSymbol))
                {
                    Write("super();");
                    WriteNewLine();
                }
                EndBlock();
            }
            EndBlock();

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken), false);
        }
    }
}
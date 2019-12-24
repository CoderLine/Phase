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

            Write("using system.TypeScriptExtensions;");
            WriteNewLine();

            WriteComments(_type.TypeSymbol, cancellationToken);

            WriteMeta(_type.TypeSymbol, cancellationToken);

            if (_type.TypeSymbol.DeclaredAccessibility == Accessibility.Public)
            {
                Write("@:expose");
                WriteNewLine();
            }

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
                if (_type.TypeSymbol.BaseType != null &&
                    _type.TypeSymbol.BaseType.SpecialType != SpecialType.System_Object)
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

            bool hasStaticConstructor = false;
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
                    if (((IMethodSymbol) member).MethodKind == MethodKind.StaticConstructor)
                    {
                        hasStaticConstructor = true;
                    }
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

            if (!Emitter.IsAbstract(_type.TypeSymbol)
                && !hasStaticConstructor
                && _type.TypeSymbol.DeclaredAccessibility == Accessibility.Public)
            {
                Write("static function __init__()");
                WriteNewLine();
                BeginBlock();

                WriteES5PropertyDeclarations(_type.TypeSymbol);

                EndBlock();
            }

            EndBlock();

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken), false);
        }
    }
}
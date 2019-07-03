using System;
using System.Threading;

namespace Phase.Translator.Kotlin
{
    public class DelegateBlock : AbstractKotlinEmitterBlock
    {
        private readonly PhaseDelegate _type;

        private string _package;
        private string _name;

        public DelegateBlock(KotlinEmitterContext emitter)
            : this(emitter.CurrentType, emitter)
        {
        }

        public DelegateBlock(PhaseType type, KotlinEmitterContext emitter)
            : base(emitter)
        {
            _type = (PhaseDelegate)type;
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

            WriteAccessibility(_type.TypeSymbol.DeclaredAccessibility);

            Write("typealias ", _name);

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


            Write(" = ");

            WriteOpenParentheses();

            var method = _type.TypeSymbol.DelegateInvokeMethod;
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                if(i> 0)WriteComma();

                Write(method.Parameters[i].Name);
                Write(" : ");
                Write(Emitter.GetTypeName(method.Parameters[i].Type, false, false));
            }

            WriteCloseParentheses();

            Write("->");

            Write(Emitter.GetTypeName(method.ReturnType, false, false));

            WriteNewLine();

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken), false);
        }
    }
}
using System;
using System.Threading;

namespace Phase.Translator.Java
{
    public class DelegateBlock : AbstractJavaEmitterBlock
    {
        private readonly PhaseDelegate _type;

        private string _package;
        private string _name;

        public DelegateBlock(JavaEmitterContext emitter)
            : this(emitter.CurrentType, emitter)
        {
        }

        public DelegateBlock(PhaseType type, JavaEmitterContext emitter)
            : base(emitter)
        {
            _type = (PhaseDelegate)type;
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

            BeginBlock();

            var method = _type.TypeSymbol.DelegateInvokeMethod;
            var methodBlock = new MethodBlock(EmitterContext, method);
            methodBlock.Emit(cancellationToken);

            EndBlock();

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken), false);
        }
    }
}
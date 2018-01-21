using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Haxe
{
    public class DelegateBlock : AbstractHaxeScriptEmitterBlock
    {
        private readonly PhaseDelegate _type;

        public DelegateBlock(HaxeEmitterContext context, PhaseDelegate type)
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
            var fullName = Emitter.GetTypeName(_type);
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

            Write("typedef ", name, " = ");

            var method = _type.TypeSymbol.DelegateInvokeMethod;

            if (method.Parameters.Length > 0)
            {
                foreach (var p in method.Parameters)
                {
                    Write("/* ");
                    Write(p.Name);
                    WriteSpace();
                    WriteColon();
                    Write("*/");
                    WriteType(p.Type);

                    Write(" -> ");
                }
            }
            else
            {
                WriteType(Emitter.GetSpecialType(SpecialType.System_Void));
                Write(" -> ");
            }
            WriteType(method.ReturnType);

            WriteSemiColon(true);

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken), false);
        }
    }
}
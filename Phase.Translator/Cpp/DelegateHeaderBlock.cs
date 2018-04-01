using System.Linq;
using System.Threading;

namespace Phase.Translator.Cpp
{
    class DelegateHeaderBlock : AbstractCppEmitterBlock
    {
        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteDefaultFileHeader();
            var type = (PhaseDelegate)EmitterContext.CurrentType;
            var method = type.TypeSymbol.DelegateInvokeMethod;

            var fullName = Emitter.GetTypeName(type.TypeSymbol);
            var parts = fullName.Split('.');

            if (parts.Length > 1)
            {
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    Write("namespace ", parts[i], "{");
                }
            }

            var name = parts.Last();

            Write("typedef std::function<");
            WriteType(method.ReturnType);
            WriteOpenParentheses();

            WriteParameterDeclarations(method.Parameters, true, cancellationToken);

            WriteCloseParentheses();
            Write("> ", name);

            WriteSemiColon(true);
        }
    }
}
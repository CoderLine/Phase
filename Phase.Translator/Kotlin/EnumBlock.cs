using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Kotlin
{
    public class EnumBlock : AbstractKotlinEmitterBlock
    {
        private readonly PhaseEnum _type;

        private string _package;
        private string _name;

        public EnumBlock(KotlinEmitterContext emitter)
            : this(emitter.CurrentType, emitter)
        {
        }

        public EnumBlock(PhaseType type, KotlinEmitterContext emitter)
            : base(emitter)
        {
            _type = (PhaseEnum)type;
            var fullName = Emitter.GetTypeName(_type.TypeSymbol, false, true, false);
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

        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
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
            Write("class ", _name, "(val value : ");
            WriteType(_type.TypeSymbol.EnumUnderlyingType);
            Write(")");
            WriteNewLine();

            BeginBlock();

            Write("companion object");
            WriteNewLine();
            BeginBlock();

            foreach (var enumMember in _type.TypeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                var enumMemberBlock = new EnumMemberBlock(EmitterContext, enumMember);
                enumMemberBlock.Emit(cancellationToken);
            }

            Write("@JvmStatic");
            WriteNewLine();
            Write("fun fromValue");
            WriteOpenParentheses();
            Write("value : ");
            WriteType(_type.TypeSymbol.EnumUnderlyingType);
            WriteCloseParentheses();

            Write(" : ", _name);
            WriteNewLine();
            BeginBlock();

            Write("when");
            WriteOpenParentheses();
            Write("value");
            WriteCloseParentheses();
            BeginBlock();

            foreach (var enumMember in _type.TypeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                Write(enumMember.ConstantValue);
                Write(" -> return ");
                Write(Emitter.GetFieldName(enumMember));
                WriteSemiColon(true);
            }

            Write("else -> return ", _name, "(value);");
            WriteNewLine();

            EndBlock();
            EndBlock();
            EndBlock();

            WriteNewLine();

            Write("operator fun inc() : ", _name, " { return fromValue(value + 1); }");
            WriteNewLine();

            Write("operator fun dec() : ", _name, " { return fromValue(value - 1); }");
            WriteNewLine();

            Write("operator fun plus(rhs : Int) : ", _name, " { return fromValue(value + rhs); }");
            WriteNewLine();

            Write("operator fun minus(rhs : Int) : ", _name, " { return fromValue(value - rhs); }");
            WriteNewLine();

            Write("operator fun plus(rhs : ", _name, ") : ", _name, " { return fromValue(value + rhs.value); }");
            WriteNewLine();

            Write("operator fun minus(rhs : ", _name, ") : ", _name, " { return fromValue(value - rhs.value); }");
            WriteNewLine();

            Write("operator fun compareTo(rhs : ", _name, ") : Int { return value.compareTo(rhs.value); }");
            WriteNewLine();

            Write("override fun equals(rhs : Any? ) : Boolean { if(rhs is ",_name,") return value.equals(rhs.value); else return false; }");
            WriteNewLine();

            Write("infix fun and(rhs : ", _name, ") : ", _name, " { return fromValue(value and rhs.value); }");
            WriteNewLine();

            Write("infix fun or(rhs : ", _name, ") : ", _name, " { return fromValue(value or rhs.value); }");
            WriteNewLine();

            Write("infix fun xor(rhs : ", _name, ") : ", _name, " { return fromValue(value xor rhs.value); }");
            WriteNewLine();

            EndBlock();

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken), false);
        }
    }
}
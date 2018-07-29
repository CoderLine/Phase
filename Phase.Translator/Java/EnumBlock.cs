using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Java
{
    public class EnumBlock : AbstractJavaEmitterBlock
    {
        private readonly PhaseEnum _type;

        private string _package;
        private string _name;

        public EnumBlock(JavaEmitterContext emitter)
            : this(emitter.CurrentType, emitter)
        {
        }

        public EnumBlock(PhaseType type, JavaEmitterContext emitter)
            : base(emitter)
        {
            _type = (PhaseEnum)type;
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
            EmitNested(cancellationToken);
        }

        public void EmitNested(CancellationToken cancellationToken)
        {
            WriteComments(_type.TypeSymbol, cancellationToken);

            WriteAccessibility(_type.TypeSymbol.DeclaredAccessibility);
            Write("enum ", _name);

            BeginBlock();

            var first = true;
            foreach (var enumMember in _type.TypeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                if (!first)
                {
                    Write(",");
                    WriteNewLine();
                }
                first = false;
                var enumMemberBlock = new EnumMemberBlock(EmitterContext, enumMember);
                enumMemberBlock.Emit(cancellationToken);
            }

            WriteSemiColon(true);

            Write("private ");
            WriteType(_type.TypeSymbol.EnumUnderlyingType);
            Write(" _value;");
            WriteNewLine();

            Write(_name);
            WriteOpenParentheses();
            WriteType(_type.TypeSymbol.EnumUnderlyingType);
            Write(" value");
            WriteCloseParentheses();
            WriteNewLine();
            BeginBlock();
            Write("_value = value;");
            WriteNewLine();
            EndBlock();

            var underlyingType = Emitter.GetTypeName(_type.TypeSymbol.EnumUnderlyingType);
            Write("public ", underlyingType, " getValue() { return _value; }");
            WriteNewLine();

            Write("public static ", _name, " fromValue");
            WriteOpenParentheses();
            Write(underlyingType, " value");
            WriteCloseParentheses();
            WriteNewLine();
            BeginBlock();

            WriteSwitch();
            WriteOpenParentheses();
            Write("value");
            WriteCloseParentheses();
            BeginBlock();

            foreach (var enumMember in _type.TypeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                Write("case ");
                Write(enumMember.ConstantValue);
                Write(": return ");
                Write(enumMember.Name);
                WriteSemiColon(true);
            }

            Write("default: throw new IllegalArgumentException(\"Invalid enum value\");");
            WriteNewLine();

            EndBlock();
            EndBlock();

            WriteNewLine();

            //Write("public static boolean greaterThan(", _name, " lhs ", _name, " rhs) { return lhs._value > rhs._value; }");
            //WriteNewLine();

            //Write("public static boolean greaterThanOrEqual(", _name, " lhs ", _name, " rhs) { return lhs._value >= rhs._value; }");
            //WriteNewLine();

            //Write("public static boolean lessThan(", _name, " lhs ", _name, " rhs) { return lhs._value < rhs._value; }");
            //WriteNewLine();

            //Write("public static boolean lessThanOrEqual(", _name, " lhs ", _name, " rhs) { return lhs._value <= rhs._value; }");
            //WriteNewLine();

            //Write("public static boolean equality(", _name, " lhs ", _name, " rhs) { return lhs._value == rhs._value; }");
            //WriteNewLine();

            //Write("public static boolean inequality(", _name, " lhs ", _name, " rhs) { return lhs._value != rhs._value; }");
            //WriteNewLine();

            //Write("public static ", underlyingType, " and(", _name, " lhs ", _name, " rhs) { return (", underlyingType, ")(lhs._value & rhs._value); }");
            //WriteNewLine();

            //Write("public static ", underlyingType, " or(", _name, " lhs ", _name, " rhs) { return (", underlyingType, ")(lhs._value | rhs._value); }");
            //WriteNewLine();

            //Write("public static ", underlyingType, " xor(", _name, " lhs ", _name, " rhs) { return (", underlyingType, ")(lhs._value ^ rhs._value); }");
            //WriteNewLine();

            EndBlock();

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken), false);
        }
    }
}
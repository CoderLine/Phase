using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Haxe
{
    public class EnumBlock : AbstractHaxeScriptEmitterBlock
    {
        private readonly PhaseEnum _type;

        public EnumBlock(HaxeEmitterContext context, PhaseEnum type)
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

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken));

            var fullName = Emitter.GetTypeName(_type.TypeSymbol);
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

            if (package.Length > 1)
            {
                Write("package ");
                Write(package);
                WriteSemiColon(true);
                WriteNewLine();
            }

            WriteComments(_type.TypeSymbol, cancellationToken);

            Write("@:enum");
            WriteNewLine();

            if (_type.TypeSymbol.DeclaredAccessibility == Accessibility.Public)
            {
                Write("@:expose");
                WriteNewLine();
            }

            Write("abstract ", name, "(Int) from Int to Int");

            BeginBlock();

            foreach (var enumMember in _type.TypeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                var enumMemberBlock = new EnumMemberBlock(EmitterContext, enumMember);
                enumMemberBlock.Emit(cancellationToken);
            }

	        Write("public inline function ToBoolean_IFormatProvider(provider:system.IFormatProvider) : system.Boolean return system.Convert.ToBoolean_Int32(this);");
            WriteNewLine();

            Write("public inline function ToChar_IFormatProvider(provider:system.IFormatProvider) : system.Char return system.Convert.ToChar_Int32(this);");
            WriteNewLine();

            Write("public inline function ToSByte_IFormatProvider(provider:system.IFormatProvider) : system.SByte return system.Convert.ToSByte_Byte(this);");
            WriteNewLine();

            Write("public inline function ToByte_IFormatProvider(provider:system.IFormatProvider) : system.Byte return system.Convert.ToByte_Int32(this);");
            WriteNewLine();

            Write("public inline function ToInt16_IFormatProvider(provider:system.IFormatProvider) : system.Int16 return system.Convert.ToInt16_Int32(this);");
            WriteNewLine();

            Write("public inline function ToUInt16_IFormatProvider(provider:system.IFormatProvider) : system.UInt16 return system.Convert.ToUInt16_Int32(this);");
            WriteNewLine();

            Write("public inline function ToInt32_IFormatProvider(provider:system.IFormatProvider) : system.Int32 return system.Convert.ToInt32_Int32(this);");
            WriteNewLine();

            Write("public inline function ToUInt32_IFormatProvider(provider:system.IFormatProvider) : system.UInt32 return system.Convert.ToUInt32_Int32(this);");
            WriteNewLine();

            Write("public inline function ToInt64_IFormatProvider(provider:system.IFormatProvider) : system.Int64 return system.Convert.ToInt64_Int32(this);");
            WriteNewLine();

            Write("public inline function ToUInt64_IFormatProvider(provider:system.IFormatProvider) : system.UInt64 return system.Convert.ToUInt64_Int32(this);");
            WriteNewLine();

            Write("public inline function ToSingle_IFormatProvider(provider:system.IFormatProvider) : system.Single return system.Convert.ToSingle_Int32(this);");
            WriteNewLine();

            Write("public inline function ToDouble_IFormatProvider(provider:system.IFormatProvider) : system.Double return system.Convert.ToDouble_Int32(this);");
            WriteNewLine();

            Write("@:op(A+B) public static function add1(lhs:", name, ", rhs:system.CsString):system.CsString;");
            WriteNewLine();

            Write("@:op(A+B) public static function add2(lhs:system.CsString, rhs:", name, "):system.CsString;");
            WriteNewLine();

            Write("@:op(A>B) public static function gt(lhs:", name, ", rhs:", name, "):system.Boolean;");
            WriteNewLine();

            Write("@:op(A>=B) public static function gte(lhs:", name, ", rhs:", name, "):system.Boolean;");
            WriteNewLine();

            Write("@:op(A<B) public static function lt(lhs:", name, ", rhs:", name, "):system.Boolean;");
            WriteNewLine();

            Write("@:op(A<=B) public static function lte(lhs:", name, ", rhs:", name, "):system.Boolean;");
            WriteNewLine();

            Write("@:op(A==B) public static function eq(lhs:", name, ", rhs:", name, "):system.Boolean;");
            WriteNewLine();

            Write("@:op(A!=B) public static function neq(lhs:", name, ", rhs:", name, "):system.Boolean;");
            WriteNewLine();

            Write("@:to public function toString() : system.CsString");
            WriteNewLine();

            BeginBlock();

            Write("switch(this)");
            WriteNewLine();
            BeginBlock();

            foreach (var enumMember in _type.TypeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                Write("case ", enumMember.Name, ": return \"", enumMember.Name, "\";");
                WriteNewLine();
            }

            EndBlock();

            Write("return \"\";");
            WriteNewLine();

            EndBlock();

            EndBlock();

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken), false);
        }
    }
}
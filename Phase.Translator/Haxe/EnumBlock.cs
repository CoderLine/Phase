using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Haxe
{
    public class EnumBlock : AbstractHaxeScriptEmitterBlock
    {
        private readonly PhaseEnum _type;

        public EnumBlock(HaxeEmitter emitter, PhaseEnum type)
            : base(emitter)
        {
            _type = type;
        }

        protected override async Task DoEmitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Emitter.IsExternal(_type.TypeSymbol))
            {
                return;
            }

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

            Write("@:enum");
            WriteNewLine();

            Write("abstract ", name, "(Int) from Int to Int");

            BeginBlock();

            foreach (var enumMember in _type.TypeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                var enumMemberBlock = new EnumMemberBlock(Emitter, enumMember);
                await enumMemberBlock.EmitAsync(cancellationToken);
            }

            Write("@:to public inline function toInt():Int return this;");
            WriteNewLine();

            Write("@:op(A+B) public static inline function add1(lhs:", name, ", rhs:String):String return lhs.toString() + rhs;");
            WriteNewLine();

            Write("@:op(A+B) public static inline function add2(lhs:String, rhs:", name, "):String return lhs + rhs.toString();");
            WriteNewLine();

            Write("@:op(A>B) public static inline function gt(lhs:", name, ", rhs:", name, "):Bool return lhs.toInt() > rhs.toInt();");
            WriteNewLine();

            Write("@:op(A>=B) public static inline function gte(lhs:", name, ", rhs:", name, "):Bool return lhs.toInt() >= rhs.toInt();");
            WriteNewLine();

            Write("@:op(A<B) public static inline function lt(lhs:", name, ", rhs:", name, "):Bool return lhs.toInt() < rhs.toInt();");
            WriteNewLine();

            Write("@:op(A<=B) public static inline function lte(lhs:", name, ", rhs:", name, "):Bool return lhs.toInt() <= rhs.toInt();");
            WriteNewLine();

            Write("@:op(A==B) public static inline function eq(lhs:", name, ", rhs:", name, "):Bool return lhs.toInt() == rhs.toInt();");
            WriteNewLine();

            Write("@:op(A!=B) public static inline function neq(lhs:", name, ", rhs:", name, "):Bool return lhs.toInt() != rhs.toInt();");
            WriteNewLine();

            Write("@:to public function toString() : String");
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
        }
    }
}
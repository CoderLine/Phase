using System;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.TypeScript
{
    public class EnumBlock : AbstractTypeScriptEmitterBlock
    {
        private readonly PhaseEnum _type;

        public EnumBlock(TypeScriptEmitterContext context)
            : base(context)
        {
            _type = (PhaseEnum) context.CurrentType;
        }

        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Emitter.IsExternal(_type.TypeSymbol))
            {
                return;
            }

            PushWriter();
            EmitEnum(cancellationToken);

            var result = PopWriter();

            foreach (var importedType in EmitterContext.ImportedTypes.Values)
            {
                WriteImport(importedType.Type);
            }

            WriteNewLine();

            Write(result);
        }

        private void EmitEnum(CancellationToken cancellationToken)
        {
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

            WriteComments(_type.TypeSymbol, cancellationToken);

            WriteMeta(_type.TypeSymbol, cancellationToken);

            Write("export enum ", name, " ");

            BeginBlock();

            foreach (var enumMember in _type.TypeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                var enumMemberBlock = new EnumMemberBlock(EmitterContext, enumMember);
                enumMemberBlock.Emit(cancellationToken);
            }

            EndBlock();

            Write("export namespace ", name, " ");
            BeginBlock();
            
            Write("export function toByte(v: ", name, ") : number { return v as number; }");
            WriteNewLine();
            
            Write("export function toInt32(v: ", name, ") : number { return v as number; }");
            WriteNewLine();
            
            Write("export function toString(v: ", name, "): string ");
            BeginBlock();

            Write("switch (v) ");
            BeginBlock();

            foreach (var enumMember in _type.TypeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                Write("case ", name, ".", enumMember.Name, ": return \"", enumMember.Name, "\";");
                WriteNewLine();
            }

            EndBlock();

            Write("return \"\";");
            WriteNewLine();

            EndBlock();


            Write("export function fromString(str: string): ", name, " ");
            BeginBlock();

            Write("switch (str.toLowerCase()) ");
            BeginBlock();

            foreach (var enumMember in _type.TypeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                Write("case \"", enumMember.Name.ToLowerInvariant(), "\": return ", name, ".", enumMember.Name, ";");
                WriteNewLine();
            }

            EndBlock();

            EmitterContext.ImportType(Emitter.GetPhaseType(typeof(ArgumentException).FullName));
            Write(
                "throw new ArgumentException().ArgumentException_string_string(\"Unsupported string value\", \"str\");");
            WriteNewLine();

            EndBlock();

            EndBlock();

            WriteComments(_type.RootNode.SyntaxTree.GetRoot(cancellationToken), false);
        }
    }
}
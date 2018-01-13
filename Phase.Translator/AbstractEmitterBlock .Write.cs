using System.Collections.Generic;
using Phase.Translator.Haxe;

namespace Phase.Translator
{
    public abstract partial class AbstractEmitterBlock : IWriter
    {
        public bool IsNewLine
        {
            get { return Emitter.Writer.IsNewLine; }
            set { Emitter.Writer.IsNewLine = value; }
        }
        public void Indent()
        {
            Emitter.Writer.Indent();
        }

        public void Outdent()
        {
            Emitter.Writer.Outdent();
        }

        public void WriteIndent()
        {
            Emitter.Writer.WriteIndent();
        }

        public void WriteNewLine()
        {
            Emitter.Writer.WriteNewLine();
        }

        public void BeginBlock()
        {
            Emitter.Writer.BeginBlock();
        }

        public void EndBlock()
        {
            Emitter.Writer.EndBlock();
        }

        public void Write(object value)
        {
            Emitter.Writer.Write(value);
        }

        public void Write(params object[] values)
        {
            Emitter.Writer.Write(values);
        }

        public void WriteLines(IEnumerable<string> lines)
        {
            Emitter.Writer.WriteLines(lines);
        }

        public void WriteLines(params string[] lines)
        {
            Emitter.Writer.WriteLines(lines);
        }

        public void WriteComma()
        {
            Emitter.Writer.WriteComma();
        }

        public void WriteComma(bool newLine)
        {
            Emitter.Writer.WriteComma(newLine);
        }

        public void WriteThis()
        {
            Emitter.Writer.WriteThis();
        }

        public void WriteSpace()
        {
            Emitter.Writer.WriteSpace();
        }

        public void WriteSpace(bool addSpace)
        {
            Emitter.Writer.WriteSpace(addSpace);
        }

        public void WriteDot()
        {
            Emitter.Writer.WriteDot();
        }

        public void WriteColon()
        {
            Emitter.Writer.WriteColon();
        }

        public void WriteSemiColon()
        {
            Emitter.Writer.WriteSemiColon();
        }

        public void WriteSemiColon(bool newLine)
        {
            Emitter.Writer.WriteSemiColon(newLine);
        }

        public void WriteNew()
        {
            Emitter.Writer.WriteNew();
        }

        public void WriteVar()
        {
            Emitter.Writer.WriteVar();
        }

        public void WriteIf()
        {
            Emitter.Writer.WriteIf();
        }

        public void WriteElse()
        {
            Emitter.Writer.WriteElse();
        }

        public void WriteWhile()
        {
            Emitter.Writer.WriteWhile();
        }

        public void WriteFor()
        {
            Emitter.Writer.WriteFor();
        }

        public void WriteThrow()
        {
            Emitter.Writer.WriteThrow();
        }

        public void WriteTry()
        {
            Emitter.Writer.WriteTry();
        }

        public void WriteCatch()
        {
            Emitter.Writer.WriteCatch();
        }

        public void WriteFinally()
        {
            Emitter.Writer.WriteFinally();
        }

        public void WriteDo()
        {
            Emitter.Writer.WriteDo();
        }

        public void WriteSwitch()
        {
            Emitter.Writer.WriteSwitch();
        }

        public void WriteReturn(bool addSpace)
        {
            Emitter.Writer.WriteReturn(addSpace);
        }

        public void WriteOpenBracket()
        {
            Emitter.Writer.WriteOpenBracket();
        }

        public void WriteOpenBracket(bool addSpace)
        {
            Emitter.Writer.WriteOpenBracket(addSpace);
        }

        public void WriteCloseBracket()
        {
            Emitter.Writer.WriteCloseBracket();
        }

        public void WriteCloseBracket(bool addSpace)
        {
            Emitter.Writer.WriteCloseBracket(addSpace);
        }

        public void WriteOpenParentheses()
        {
            Emitter.Writer.WriteOpenParentheses();
        }

        public void WriteOpenParentheses(bool addSpace)
        {
            Emitter.Writer.WriteOpenParentheses(addSpace);
        }

        public void WriteCloseParentheses()
        {
            Emitter.Writer.WriteCloseParentheses();
        }

        public void WriteCloseParentheses(bool addSpace)
        {
            Emitter.Writer.WriteCloseParentheses(addSpace);
        }

        public void WriteOpenCloseParentheses()
        {
            Emitter.Writer.WriteOpenCloseParentheses();
        }

        public void WriteOpenCloseParentheses(bool addSpace)
        {
            Emitter.Writer.WriteOpenCloseParentheses(addSpace);
        }

        public void WriteOpenBrace()
        {
            Emitter.Writer.WriteOpenBrace();
        }

        public void WriteOpenBrace(bool addSpace)
        {
            Emitter.Writer.WriteOpenBrace(addSpace);
        }

        public void WriteCloseBrace()
        {
            Emitter.Writer.WriteCloseBrace();
        }

        public void WriteCloseBrace(bool addSpace)
        {
            Emitter.Writer.WriteCloseBrace(addSpace);
        }

        public void WriteOpenCloseBrace()
        {
            Emitter.Writer.WriteOpenCloseBrace();
        }

        public void WriteFunction()
        {
            Emitter.Writer.WriteFunction();
        }
    }
}

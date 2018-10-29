using System.Collections.Generic;

namespace Phase.Translator
{
    public abstract partial class AbstractEmitterBlock : IWriter
    {
        protected abstract IWriter Writer { get; }

        public bool IsNewLine
        {
            get { return Writer.IsNewLine; }
            set { Writer.IsNewLine = value; }
        }

        public int Level
        {
            get => Writer.Level;
            set => Writer.Level = value;
        }

        public void Indent()
        {
            Writer.Indent();
        }

        public void Outdent()
        {
            Writer.Outdent();
        }

        public void WriteIndent()
        {
            Writer.WriteIndent();
        }

        public void WriteNewLine()
        {
            Writer.WriteNewLine();
        }

        public void BeginBlock()
        {
            Writer.BeginBlock();
        }

        public void EndBlock(bool newline = true)
        {
            Writer.EndBlock(newline);
        }

        public void Write(object value)
        {
            Writer.Write(value);
        }

        public void Write(params object[] values)
        {
            Writer.Write(values);
        }

        public void WriteLines(IEnumerable<string> lines)
        {
            Writer.WriteLines(lines);
        }

        public void WriteLines(params string[] lines)
        {
            Writer.WriteLines(lines);
        }

        public void WriteComma()
        {
            Writer.WriteComma();
        }

        public void WriteComma(bool newLine)
        {
            Writer.WriteComma(newLine);
        }

        public void WriteThis()
        {
            Writer.WriteThis();
        }

        public void WriteSpace()
        {
            Writer.WriteSpace();
        }

        public void WriteSpace(bool addSpace)
        {
            Writer.WriteSpace(addSpace);
        }

        public void WriteDot()
        {
            Writer.WriteDot();
        }

        public void WriteColon()
        {
            Writer.WriteColon();
        }

        public void WriteSemiColon()
        {
            Writer.WriteSemiColon();
        }

        public void WriteSemiColon(bool newLine)
        {
            Writer.WriteSemiColon(newLine);
        }

        public void WriteNew()
        {
            Writer.WriteNew();
        }

        public void WriteVar()
        {
            Writer.WriteVar();
        }

        public void WriteIf()
        {
            Writer.WriteIf();
        }

        public void WriteElse()
        {
            Writer.WriteElse();
        }

        public void WriteWhile()
        {
            Writer.WriteWhile();
        }

        public void WriteFor()
        {
            Writer.WriteFor();
        }

        public void WriteThrow()
        {
            Writer.WriteThrow();
        }

        public void WriteTry()
        {
            Writer.WriteTry();
        }

        public void WriteCatch()
        {
            Writer.WriteCatch();
        }

        public void WriteFinally()
        {
            Writer.WriteFinally();
        }

        public void WriteDo()
        {
            Writer.WriteDo();
        }

        public void WriteSwitch()
        {
            Writer.WriteSwitch();
        }

        public void WriteReturn(bool addSpace)
        {
            Writer.WriteReturn(addSpace);
        }

        public void WriteOpenBracket()
        {
            Writer.WriteOpenBracket();
        }

        public void WriteOpenBracket(bool addSpace)
        {
            Writer.WriteOpenBracket(addSpace);
        }

        public void WriteCloseBracket()
        {
            Writer.WriteCloseBracket();
        }

        public void WriteCloseBracket(bool addSpace)
        {
            Writer.WriteCloseBracket(addSpace);
        }

        public void WriteOpenParentheses()
        {
            Writer.WriteOpenParentheses();
        }

        public void WriteOpenParentheses(bool addSpace)
        {
            Writer.WriteOpenParentheses(addSpace);
        }

        public void WriteCloseParentheses()
        {
            Writer.WriteCloseParentheses();
        }

        public void WriteCloseParentheses(bool addSpace)
        {
            Writer.WriteCloseParentheses(addSpace);
        }

        public void WriteOpenCloseParentheses()
        {
            Writer.WriteOpenCloseParentheses();
        }

        public void WriteOpenCloseParentheses(bool addSpace)
        {
            Writer.WriteOpenCloseParentheses(addSpace);
        }

        public void WriteOpenBrace()
        {
            Writer.WriteOpenBrace();
        }

        public void WriteOpenBrace(bool addSpace)
        {
            Writer.WriteOpenBrace(addSpace);
        }

        public void WriteCloseBrace()
        {
            Writer.WriteCloseBrace();
        }

        public void WriteCloseBrace(bool addSpace)
        {
            Writer.WriteCloseBrace(addSpace);
        }

        public void WriteOpenCloseBrace()
        {
            Writer.WriteOpenCloseBrace();
        }

        public void WriteFunction()
        {
            Writer.WriteFunction();
        }
    }
}

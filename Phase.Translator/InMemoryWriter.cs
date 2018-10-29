using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Phase.Translator.Haxe;

namespace Phase.Translator
{
    public class InMemoryWriter : IWriter
    {
        private readonly StringBuilder _output;

        public InMemoryWriter()
        {
            _output = new StringBuilder();
        }

        public bool IsNewLine { get; set; }
        public int Level { get; set; }

        public void Indent()
        {
            Level++;
        }

        public void Outdent()
        {
            if (Level > 0)
            {
                Level--;
            }
        }

        public void WriteIndent()
        {
            if (!IsNewLine)
            {
                return;
            }

            for (var i = 0; i < Level; i++)
            {
                _output.Append("    ");
            }

            IsNewLine = false;
        }

        public void WriteNewLine()
        {
            _output.Append(Environment.NewLine);
            IsNewLine = true;
        }

        public void BeginBlock()
        {
            WriteOpenBrace();
            WriteNewLine();
            Indent();
        }

        public void EndBlock(bool newline = true)
        {
            Outdent();
            WriteCloseBrace();
            if (newline)
            {
                WriteNewLine();
            }
        }

        public void Write(object value)
        {
            WriteIndent();
            _output.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        public void Write(params object[] values)
        {
            foreach (var item in values)
            {
                Write(item);
            }
        }

        public void WriteLines(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                Write(line);
                WriteNewLine();
            }
        }

        public void WriteLines(params string[] lines)
        {
            WriteLines((IEnumerable<string>)lines);
        }

        public void WriteComma()
        {
            WriteComma(false);
        }

        public void WriteComma(bool newLine)
        {
            Write(",");

            if (newLine)
            {
                WriteNewLine();
            }
            else
            {
                WriteSpace();
            }
        }

        public void WriteThis()
        {
            Write("this");
        }

        public void WriteSpace()
        {
            WriteSpace(true);
        }

        public void WriteSpace(bool addSpace)
        {
            if (addSpace)
            {
                Write(" ");
            }
        }

        public void WriteDot()
        {
            Write(".");
        }

        public void WriteColon()
        {
            Write(": ");
        }

        public void WriteSemiColon()
        {
            WriteSemiColon(false);
        }

        public void WriteSemiColon(bool newLine)
        {
            Write(";");

            if (newLine)
            {
                WriteNewLine();
            }
        }

        public void WriteNew()
        {
            Write("new ");
        }

        public void WriteVar()
        {
            Write("var ");
        }

        public void WriteIf()
        {
            Write("if ");
        }

        public void WriteElse()
        {
            Write("else ");
        }

        public void WriteWhile()
        {
            Write("while ");
        }

        public void WriteFor()
        {
            Write("for ");
        }

        public void WriteThrow()
        {
            Write("throw ");
        }

        public virtual void WriteTry()
        {
            Write("try ");
        }

        public virtual void WriteCatch()
        {
            Write("catch ");
        }

        public virtual void WriteFinally()
        {
            Write("finally ");
        }

        public virtual void WriteDo()
        {
            Write("do");
        }

        public virtual void WriteSwitch()
        {
            Write("switch ");
        }

        public virtual void WriteReturn(bool addSpace)
        {
            Write("return");
            WriteSpace(addSpace);
        }

        public virtual void WriteOpenBracket()
        {
            WriteOpenBracket(false);
        }

        public virtual void WriteOpenBracket(bool addSpace)
        {
            Write("[");
            WriteSpace(addSpace);
        }

        public virtual void WriteCloseBracket()
        {
            WriteCloseBracket(false);
        }

        public virtual void WriteCloseBracket(bool addSpace)
        {
            WriteSpace(addSpace);
            Write("]");
        }

        public virtual void WriteOpenParentheses()
        {
            WriteOpenParentheses(false);
        }

        public virtual void WriteOpenParentheses(bool addSpace)
        {
            Write("(");
            WriteSpace(addSpace);
        }

        public virtual void WriteCloseParentheses()
        {
            WriteCloseParentheses(false);
        }

        public virtual void WriteCloseParentheses(bool addSpace)
        {
            WriteSpace(addSpace);
            Write(")");
        }

        public virtual void WriteOpenCloseParentheses()
        {
            WriteOpenCloseParentheses(false);
        }

        public virtual void WriteOpenCloseParentheses(bool addSpace)
        {
            Write("()");
            WriteSpace(addSpace);
        }

        public virtual void WriteOpenBrace()
        {
            WriteOpenBrace(false);
        }

        public virtual void WriteOpenBrace(bool addSpace)
        {
            Write("{");
            WriteSpace(addSpace);
        }

        public virtual void WriteCloseBrace()
        {
            WriteCloseBrace(false);
        }

        public virtual void WriteCloseBrace(bool addSpace)
        {
            WriteSpace(addSpace);
            Write("}");
        }

        public virtual void WriteOpenCloseBrace()
        {
            Write("{ }");
        }

        public virtual void WriteFunction()
        {
            Write("function ");
        }

        public override string ToString()
        {
            return _output.ToString();
        }
    }
}

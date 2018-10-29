using System.Collections.Generic;

namespace Phase.Translator
{
    public interface IWriter
    {
        bool IsNewLine { get; set; }

        int Level { get; set; }
        void Indent();
        void Outdent();

        void WriteIndent();
        void WriteNewLine();
        void BeginBlock();
        void EndBlock(bool newline = true);

        void Write(object value);
        void Write(params object[] values);
        void WriteLines(IEnumerable<string> lines);
        void WriteLines(params string[] lines);
        void WriteComma();
        void WriteComma(bool newLine);
        void WriteThis();
        void WriteSpace();
        void WriteSpace(bool addSpace);
        void WriteDot();
        void WriteColon();
        void WriteSemiColon();
        void WriteSemiColon(bool newLine);
        void WriteNew();
        void WriteVar();
        void WriteIf();
        void WriteElse();
        void WriteWhile();
        void WriteFor();
        void WriteThrow();
        void WriteTry();
        void WriteCatch();
        void WriteFinally();
        void WriteDo();
        void WriteSwitch();
        void WriteReturn(bool addSpace);
        void WriteOpenBracket();
        void WriteOpenBracket(bool addSpace);
        void WriteCloseBracket();
        void WriteCloseBracket(bool addSpace);
        void WriteOpenParentheses();
        void WriteOpenParentheses(bool addSpace);
        void WriteCloseParentheses();
        void WriteCloseParentheses(bool addSpace);
        void WriteOpenCloseParentheses();
        void WriteOpenCloseParentheses(bool addSpace);
        void WriteOpenBrace();
        void WriteOpenBrace(bool addSpace);
        void WriteCloseBrace();
        void WriteCloseBrace(bool addSpace);
        void WriteOpenCloseBrace();
        void WriteFunction();
    }
}
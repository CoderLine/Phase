//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.Xml
{
    [External]
    [Name("haxe.xml.Printer")]
    public partial class Printer
    {
        [Name("output")]
        protected virtual extern Haxe.root.StringBuf Output { get; set; }
        [Name("pretty")]
        protected virtual extern Haxe.HaxeBool Pretty { get; set; }
        [Name("writeNode")]
        protected virtual extern void WriteNode(Haxe.root.Xml value, string tabs);
        [Name("write")]
        protected virtual extern void Write(string input);
        [Name("newline")]
        protected virtual extern void Newline();
        [Name("hasChildren")]
        protected virtual extern Haxe.HaxeBool HasChildren(Haxe.root.Xml value);
        [Name("new")]
        protected virtual extern void New(bool pretty);
        [Name("print")]
        public static extern Haxe.HaxeString Print(Haxe.root.Xml xml, bool pretty = false);
    }
}
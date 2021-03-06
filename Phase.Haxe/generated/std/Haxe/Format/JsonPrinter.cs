//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.Format
{
    [External]
    [Name("haxe.format.JsonPrinter")]
    public partial class JsonPrinter
    {
        [Name("buf")]
        protected virtual extern Haxe.root.StringBuf Buf { get; set; }
        [Name("replacer")]
        protected virtual extern Func<dynamic, dynamic, dynamic> Replacer { get; set; }
        [Name("indent")]
        protected virtual extern Haxe.HaxeString Indent { get; set; }
        [Name("pretty")]
        protected virtual extern Haxe.HaxeBool Pretty { get; set; }
        [Name("nind")]
        protected virtual extern Haxe.HaxeInt Nind { get; set; }
        [Name("ipad")]
        protected virtual extern void Ipad();
        [Name("newl")]
        protected virtual extern void Newl();
        [Name("write")]
        protected virtual extern void Write(object k, object v);
        [Name("addChar")]
        protected virtual extern void AddChar(int c);
        [Name("add")]
        protected virtual extern void Add(string v);
        [Name("objString")]
        protected virtual extern void ObjString(object v);
        [Name("fieldsString")]
        protected virtual extern void FieldsString(object v, Haxe.HaxeArray<string> fields);
        [Name("quote")]
        protected virtual extern void Quote(string s);
        [Name("new")]
        protected virtual extern void New(Func<object, object, object> replacer, string space);
        [Name("print")]
        public static extern Haxe.HaxeString Print(object o, Func<object, object, object> replacer = default(Func<object, object, object>), string space = default(string));
    }
}

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe
{
    [External]
    [Name("haxe.Utf8")]
    public partial class Utf8
    {
        [Name("__b")]
        protected virtual extern Haxe.HaxeString __b { get; set; }
        [Name("addChar")]
        public virtual extern void AddChar(int c);
        [Name("toString")]
        public virtual extern Haxe.HaxeString ToString();
        [Name("new")]
        public virtual extern void New(int size = default(int));
        [Name("iter")]
        public static extern void Iter(string s, Action<int> chars);
        [Name("encode")]
        public static extern Haxe.HaxeString Encode(string s);
        [Name("decode")]
        public static extern Haxe.HaxeString Decode(string s);
        [Name("charCodeAt")]
        public static extern Haxe.HaxeInt CharCodeAt(string s, int index);
        [Name("validate")]
        public static extern Haxe.HaxeBool Validate(string s);
        [Name("length")]
        public static extern Haxe.HaxeInt Length(string s);
        [Name("compare")]
        public static extern Haxe.HaxeInt Compare(string a, string b);
        [Name("sub")]
        public static extern Haxe.HaxeString Sub(string s, int pos, int len);
    }
}

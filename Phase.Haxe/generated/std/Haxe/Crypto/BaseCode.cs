//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.Crypto
{
    [External]
    [Name("haxe.crypto.BaseCode")]
    public partial class BaseCode
    {
        [Name("base")]
        protected virtual extern Haxe.Io.Bytes Base { get; set; }
        [Name("nbits")]
        protected virtual extern Haxe.HaxeInt Nbits { get; set; }
        [Name("tbl")]
        protected virtual extern Haxe.HaxeArray<Haxe.HaxeInt> Tbl { get; set; }
        [Name("encodeBytes")]
        public virtual extern Haxe.Io.Bytes EncodeBytes(Haxe.Io.Bytes b);
        [Name("initTable")]
        protected virtual extern void InitTable();
        [Name("decodeBytes")]
        public virtual extern Haxe.Io.Bytes DecodeBytes(Haxe.Io.Bytes b);
        [Name("encodeString")]
        public virtual extern Haxe.HaxeString EncodeString(string s);
        [Name("decodeString")]
        public virtual extern Haxe.HaxeString DecodeString(string s);
        [Name("new")]
        public virtual extern void New(Haxe.Io.Bytes @base);
        [Name("encode")]
        public static extern Haxe.HaxeString Encode(string s, string @base);
        [Name("decode")]
        public static extern Haxe.HaxeString Decode(string s, string @base);
    }
}
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.Io
{
    [External]
    [Name("haxe.io.BytesBuffer")]
    public partial class BytesBuffer
    {
        [Name("b")]
        protected virtual extern Haxe.HaxeArray<Haxe.HaxeInt> B { get; set; }
        [Name("length")]
        public virtual extern Haxe.HaxeInt Length { get; private set; }
        [Name("get_length")]
        protected virtual extern Haxe.HaxeInt Get_length();
        [Name("addByte")]
        public virtual extern void AddByte(int @byte);
        [Name("add")]
        public virtual extern void Add(Haxe.Io.Bytes src);
        [Name("addString")]
        public virtual extern void AddString(string v);
        [Name("addInt32")]
        public virtual extern void AddInt32(int v);
        [Name("addInt64")]
        public virtual extern void AddInt64(Haxe.Int64 v);
        [Name("addFloat")]
        public virtual extern void AddFloat(double v);
        [Name("addDouble")]
        public virtual extern void AddDouble(double v);
        [Name("addBytes")]
        public virtual extern void AddBytes(Haxe.Io.Bytes src, int pos, int len);
        [Name("getBytes")]
        public virtual extern Haxe.Io.Bytes GetBytes();
        [Name("new")]
        public virtual extern void New();
    }
}

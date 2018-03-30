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
    [Name("haxe.io.Input")]
    public partial class Input
    {
        [Name("bigEndian")]
        public virtual extern Haxe.HaxeBool BigEndian { get; set; }
        [Name("readByte")]
        public virtual extern Haxe.HaxeInt ReadByte();
        [Name("readBytes")]
        public virtual extern Haxe.HaxeInt ReadBytes(Haxe.Io.Bytes s, int pos, int len);
        [Name("close")]
        public virtual extern void Close();
        [Name("set_bigEndian")]
        protected virtual extern Haxe.HaxeBool Set_bigEndian(bool b);
        [Name("readAll")]
        public virtual extern Haxe.Io.Bytes ReadAll(int bufsize = default(int));
        [Name("readFullBytes")]
        public virtual extern void ReadFullBytes(Haxe.Io.Bytes s, int pos, int len);
        [Name("read")]
        public virtual extern Haxe.Io.Bytes Read(int nbytes);
        [Name("readUntil")]
        public virtual extern Haxe.HaxeString ReadUntil(int end);
        [Name("readLine")]
        public virtual extern Haxe.HaxeString ReadLine();
        [Name("readFloat")]
        public virtual extern Haxe.HaxeFloat ReadFloat();
        [Name("readDouble")]
        public virtual extern Haxe.HaxeFloat ReadDouble();
        [Name("readInt8")]
        public virtual extern Haxe.HaxeInt ReadInt8();
        [Name("readInt16")]
        public virtual extern Haxe.HaxeInt ReadInt16();
        [Name("readUInt16")]
        public virtual extern Haxe.HaxeInt ReadUInt16();
        [Name("readInt24")]
        public virtual extern Haxe.HaxeInt ReadInt24();
        [Name("readUInt24")]
        public virtual extern Haxe.HaxeInt ReadUInt24();
        [Name("readInt32")]
        public virtual extern Haxe.HaxeInt ReadInt32();
        [Name("readString")]
        public virtual extern Haxe.HaxeString ReadString(int len);
        [Name("getDoubleSig")]
        protected virtual extern Haxe.HaxeFloat GetDoubleSig(Haxe.HaxeArray<int> bytes);
    }
}

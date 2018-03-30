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
    [Name("haxe.crypto.Hmac")]
    public partial class Hmac
    {
        [Name("method")]
        protected virtual extern Haxe.Crypto.HashMethod Method { get; set; }
        [Name("blockSize")]
        protected virtual extern Haxe.HaxeInt BlockSize { get; set; }
        [Name("length")]
        protected virtual extern Haxe.HaxeInt Length { get; set; }
        [Name("doHash")]
        protected virtual extern Haxe.Io.Bytes DoHash(Haxe.Io.Bytes b);
        [Name("nullPad")]
        protected virtual extern Haxe.Io.Bytes NullPad(Haxe.Io.Bytes s, int chunkLen);
        [Name("make")]
        public virtual extern Haxe.Io.Bytes Make(Haxe.Io.Bytes key, Haxe.Io.Bytes msg);
        [Name("new")]
        public virtual extern void New(Haxe.Crypto.HashMethod hashMethod);
    }
}

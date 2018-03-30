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
    [Name("haxe.crypto.Sha256")]
    public partial class Sha256
    {
        [Name("doEncode")]
        protected virtual extern Haxe.HaxeArray<Haxe.HaxeInt> DoEncode(Haxe.HaxeArray<int> m, int l);
        [Name("S")]
        protected virtual extern Haxe.HaxeInt S(int X, int n);
        [Name("R")]
        protected virtual extern Haxe.HaxeInt R(int X, int n);
        [Name("Ch")]
        protected virtual extern Haxe.HaxeInt Ch(int x, int y, int z);
        [Name("Maj")]
        protected virtual extern Haxe.HaxeInt Maj(int x, int y, int z);
        [Name("Sigma0256")]
        protected virtual extern Haxe.HaxeInt Sigma0256(int x);
        [Name("Sigma1256")]
        protected virtual extern Haxe.HaxeInt Sigma1256(int x);
        [Name("Gamma0256")]
        protected virtual extern Haxe.HaxeInt Gamma0256(int x);
        [Name("Gamma1256")]
        protected virtual extern Haxe.HaxeInt Gamma1256(int x);
        [Name("safeAdd")]
        protected virtual extern Haxe.HaxeInt SafeAdd(int x, int y);
        [Name("hex")]
        protected virtual extern Haxe.HaxeString Hex(Haxe.HaxeArray<int> a);
        [Name("new")]
        public virtual extern void New();
        [Name("encode")]
        public static extern Haxe.HaxeString Encode(string s);
        [Name("make")]
        public static extern Haxe.Io.Bytes Make(Haxe.Io.Bytes b);
        [Name("str2blks")]
        private static extern Haxe.HaxeArray<Haxe.HaxeInt> Str2blks(string s);
        [Name("bytes2blks")]
        private static extern Haxe.HaxeArray<Haxe.HaxeInt> Bytes2blks(Haxe.Io.Bytes b);
    }
}

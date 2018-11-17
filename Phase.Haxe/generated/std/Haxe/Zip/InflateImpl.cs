//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.Zip
{
    [External]
    [Name("haxe.zip.InflateImpl")]
    public partial class InflateImpl
    {
        [Name("nbits")]
        protected virtual extern Haxe.HaxeInt Nbits { get; set; }
        [Name("bits")]
        protected virtual extern Haxe.HaxeInt Bits { get; set; }
        [Name("state")]
        protected virtual extern State State { get; set; }
        [Name("final")]
        protected virtual extern Haxe.HaxeBool Final { get; set; }
        [Name("huffman")]
        protected virtual extern Haxe.Zip.Huffman Huffman { get; set; }
        [Name("huffdist")]
        protected virtual extern Haxe.root.Null<Haxe.Zip.Huffman> Huffdist { get; set; }
        [Name("htools")]
        protected virtual extern Haxe.Zip.HuffTools Htools { get; set; }
        [Name("len")]
        protected virtual extern Haxe.HaxeInt Len { get; set; }
        [Name("dist")]
        protected virtual extern Haxe.HaxeInt Dist { get; set; }
        [Name("needed")]
        protected virtual extern Haxe.HaxeInt Needed { get; set; }
        [Name("output")]
        protected virtual extern Haxe.Io.Bytes Output { get; set; }
        [Name("outpos")]
        protected virtual extern Haxe.HaxeInt Outpos { get; set; }
        [Name("input")]
        protected virtual extern Haxe.Io.Input Input { get; set; }
        [Name("lengths")]
        protected virtual extern Haxe.HaxeArray<Haxe.HaxeInt> Lengths { get; set; }
        [Name("window")]
        protected virtual extern Window Window { get; set; }
        [Name("buildFixedHuffman")]
        protected virtual extern Haxe.Zip.Huffman BuildFixedHuffman();
        [Name("readBytes")]
        public virtual extern Haxe.HaxeInt ReadBytes(Haxe.Io.Bytes b, int pos, int len);
        [Name("getBits")]
        protected virtual extern Haxe.HaxeInt GetBits(int n);
        [Name("getBit")]
        protected virtual extern Haxe.HaxeBool GetBit();
        [Name("getRevBits")]
        protected virtual extern Haxe.HaxeInt GetRevBits(int n);
        [Name("resetBits")]
        protected virtual extern void ResetBits();
        [Name("addBytes")]
        protected virtual extern void AddBytes(Haxe.Io.Bytes b, int p, int len);
        [Name("addByte")]
        protected virtual extern void AddByte(int b);
        [Name("addDistOne")]
        protected virtual extern void AddDistOne(int n);
        [Name("addDist")]
        protected virtual extern void AddDist(int d, int len);
        [Name("applyHuffman")]
        protected virtual extern Haxe.HaxeInt ApplyHuffman(Haxe.Zip.Huffman h);
        [Name("inflateLengths")]
        protected virtual extern void InflateLengths(Haxe.HaxeArray<int> a, int max);
        [Name("inflateLoop")]
        protected virtual extern Haxe.HaxeBool InflateLoop();
        [Name("new")]
        public virtual extern void New(Haxe.Io.Input i, bool header = true, bool crc = true);
        [Name("LEN_EXTRA_BITS_TBL")]
        private static extern Haxe.HaxeArray<Haxe.HaxeInt> LEN_EXTRA_BITS_TBL { get; set; }
        [Name("LEN_BASE_VAL_TBL")]
        private static extern Haxe.HaxeArray<Haxe.HaxeInt> LEN_BASE_VAL_TBL { get; set; }
        [Name("DIST_EXTRA_BITS_TBL")]
        private static extern Haxe.HaxeArray<Haxe.HaxeInt> DIST_EXTRA_BITS_TBL { get; set; }
        [Name("DIST_BASE_VAL_TBL")]
        private static extern Haxe.HaxeArray<Haxe.HaxeInt> DIST_BASE_VAL_TBL { get; set; }
        [Name("CODE_LENGTHS_POS")]
        private static extern Haxe.HaxeArray<Haxe.HaxeInt> CODE_LENGTHS_POS { get; set; }
        [Name("FIXED_HUFFMAN")]
        private static extern Haxe.Zip.Huffman FIXED_HUFFMAN { get; set; }
        [Name("run")]
        public static extern Haxe.Io.Bytes Run(Haxe.Io.Input i, int bufsize = 65536);
    }
}
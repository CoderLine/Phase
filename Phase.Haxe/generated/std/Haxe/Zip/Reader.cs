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
    [Name("haxe.zip.Reader")]
    public partial class Reader
    {
        [Name("i")]
        protected virtual extern Haxe.Io.Input I { get; set; }
        [Name("readZipDate")]
        protected virtual extern Haxe.root.Date ReadZipDate();
        [Name("readExtraFields")]
        protected virtual extern Haxe.root.List<Haxe.Zip.ExtraField> ReadExtraFields(int length);
        [Name("readEntryHeader")]
        public virtual extern Haxe.Zip.Entry ReadEntryHeader();
        [Name("read")]
        public virtual extern Haxe.root.List<Haxe.Zip.Entry> Read();
        [Name("new")]
        public virtual extern void New(Haxe.Io.Input i);
        [Name("readZip")]
        public static extern Haxe.root.List<Haxe.Zip.Entry> ReadZip(Haxe.Io.Input i);
        [Name("unzip")]
        public static extern Haxe.root.Null<Haxe.Io.Bytes> Unzip(Haxe.Zip.Entry f);
    }
}

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.root
{
    [External]
    [Name("EReg")]
    public partial class EReg
    {
        [Name("r")]
        protected virtual extern _EReg.HaxeRegExp R { get; set; }
        [Name("match")]
        public virtual extern Haxe.HaxeBool Match(string s);
        [Name("matched")]
        public virtual extern Haxe.HaxeString Matched(int n);
        [Name("matchedLeft")]
        public virtual extern Haxe.HaxeString MatchedLeft();
        [Name("matchedRight")]
        public virtual extern Haxe.HaxeString MatchedRight();
        [Name("matchedPos")]
        public virtual extern dynamic MatchedPos();
        [Name("matchSub")]
        public virtual extern Haxe.HaxeBool MatchSub(string s, int pos, int len = -1);
        [Name("split")]
        public virtual extern Haxe.HaxeArray<Haxe.HaxeString> Split(string s);
        [Name("replace")]
        public virtual extern Haxe.HaxeString Replace(string s, string by);
        [Name("map")]
        public virtual extern Haxe.HaxeString Map(string s, Func<Haxe.root.EReg, string> f);
        [Name("new")]
        public virtual extern void New(string r, string opt);
    }
}
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.Ds
{
    [External]
    [Name("haxe.ds.StringMap")]
    public partial class StringMap<T>
        : Haxe.IMap<Haxe.HaxeString, T>
    {
        [Name("h")]
        protected virtual extern dynamic H { get; set; }
        [Name("rh")]
        protected virtual extern dynamic Rh { get; set; }
        [Name("isReserved")]
        protected virtual extern Haxe.HaxeBool IsReserved(string key);
        [Name("set")]
        public virtual extern void Set(string key, T value);
        [Name("get")]
        public virtual extern Haxe.root.Null<T> Get(string key);
        [Name("exists")]
        public virtual extern Haxe.HaxeBool Exists(string key);
        [Name("setReserved")]
        protected virtual extern void SetReserved(string key, T value);
        [Name("getReserved")]
        protected virtual extern Haxe.root.Null<T> GetReserved(string key);
        [Name("existsReserved")]
        protected virtual extern Haxe.HaxeBool ExistsReserved(string key);
        [Name("remove")]
        public virtual extern Haxe.HaxeBool Remove(string key);
        [Name("keys")]
        public virtual extern Haxe.root.Iterator<Haxe.HaxeString> Keys();
        [Name("arrayKeys")]
        protected virtual extern Haxe.HaxeArray<Haxe.HaxeString> ArrayKeys();
        [Name("iterator")]
        public virtual extern Haxe.root.Iterator<T> Iterator();
        [Name("toString")]
        public virtual extern Haxe.HaxeString ToString();
        [Name("new")]
        public virtual extern void New();
    }
}

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
    [Name("haxe.IMap")]
    public interface IMap<K, V>
    {
        [Name("get")]
        public virtual extern Haxe.root.Null<V> Get(K k);
        [Name("set")]
        public virtual extern void Set(K k, V v);
        [Name("exists")]
        public virtual extern Haxe.HaxeBool Exists(K k);
        [Name("remove")]
        public virtual extern Haxe.HaxeBool Remove(K k);
        [Name("keys")]
        public virtual extern Haxe.root.Iterator<K> Keys();
        [Name("iterator")]
        public virtual extern Haxe.root.Iterator<V> Iterator();
        [Name("toString")]
        public virtual extern Haxe.HaxeString ToString();
    }
}
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
    [Name("haxe.ds.WeakMap")]
    public partial class WeakMap<K, V>
        : Haxe.IMap<K, V>
    {
        [Name("set")]
        public virtual extern void Set(K key, V value);
        [Name("get")]
        public virtual extern Haxe.root.Null<V> Get(K key);
        [Name("exists")]
        public virtual extern Haxe.HaxeBool Exists(K key);
        [Name("remove")]
        public virtual extern Haxe.HaxeBool Remove(K key);
        [Name("keys")]
        public virtual extern Haxe.root.Iterator<K> Keys();
        [Name("iterator")]
        public virtual extern Haxe.root.Iterator<V> Iterator();
        [Name("toString")]
        public virtual extern Haxe.HaxeString ToString();
        [Name("new")]
        public virtual extern void New();
    }
}

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.Rtti
{
    [External]
    [Name("haxe.rtti.Meta")]
    public partial class Meta
    {
        [Name("getType")]
        public static extern dynamic GetType(object t);
        [Name("isInterface")]
        private static extern Haxe.HaxeBool IsInterface(object t);
        [Name("getMeta")]
        private static extern MetaObject GetMeta(object t);
        [Name("getStatics")]
        public static extern dynamic GetStatics(object t);
        [Name("getFields")]
        public static extern dynamic GetFields(object t);
    }
}

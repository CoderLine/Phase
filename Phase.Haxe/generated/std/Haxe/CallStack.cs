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
    [Name("haxe.CallStack")]
    public partial class CallStack
    {
        [Name("lastException")]
        private static extern Js.Error LastException { get; set; }
        [Name("getStack")]
        private static extern Haxe.HaxeArray<Haxe.StackItem> GetStack(Js.Error e);
        [Name("wrapCallSite")]
        public static extern Func<dynamic, dynamic> WrapCallSite { get; set; }
        [Name("callStack")]
        public static extern Haxe.HaxeArray<Haxe.StackItem> _CallStack();
        [Name("exceptionStack")]
        public static extern Haxe.HaxeArray<Haxe.StackItem> ExceptionStack();
        [Name("toString")]
        public static extern Haxe.HaxeString ToString(Haxe.HaxeArray<Haxe.StackItem> stack);
        [Name("itemToString")]
        private static extern void ItemToString(Haxe.root.StringBuf b, Haxe.StackItem s);
        [Name("makeStack")]
        private static extern Haxe.HaxeArray<Haxe.StackItem> MakeStack(dynamic s);
    }
}

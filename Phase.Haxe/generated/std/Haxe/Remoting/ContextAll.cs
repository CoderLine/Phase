//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.Remoting
{
    [External]
    [Name("haxe.remoting.ContextAll")]
    public partial class ContextAll
        : Haxe.Remoting.Context
    {
        [Name("call")]
        public override extern dynamic Call(Haxe.HaxeArray<string> path, Haxe.HaxeArray<object> @params);
        [Name("new")]
        public virtual extern void New();
    }
}
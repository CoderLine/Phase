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
    [Name("haxe.MainEvent")]
    public partial class MainEvent
    {
        [Name("f")]
        protected virtual extern Action<> F { get; set; }
        [Name("prev")]
        protected virtual extern MainEvent Prev { get; set; }
        [Name("next")]
        protected virtual extern MainEvent Next { get; set; }
        [Name("nextRun")]
        public virtual extern Haxe.HaxeFloat NextRun { get; private set; }
        [Name("priority")]
        public virtual extern Haxe.HaxeInt Priority { get; private set; }
        [Name("delay")]
        public virtual extern void Delay(Haxe.root.Null<double> t);
        [Name("call")]
        public virtual extern void Call();
        [Name("stop")]
        public virtual extern void Stop();
        [Name("new")]
        protected virtual extern void New(Action<> f, int p);
    }
}
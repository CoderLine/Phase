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
    [Name("IntIterator")]
    public partial class IntIterator
    {
        [Name("min")]
        protected virtual extern Haxe.HaxeInt Min { get; set; }
        [Name("max")]
        protected virtual extern Haxe.HaxeInt Max { get; set; }
        [Name("hasNext")]
        public virtual extern Haxe.HaxeBool HasNext();
        [Name("next")]
        public virtual extern Haxe.HaxeInt Next();
        [Name("new")]
        public virtual extern void New(int min, int max);
    }
}

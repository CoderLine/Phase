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
    [Name("haxe.ds.GenericCell")]
    public partial class GenericCell<T>
    {
        [Name("elt")]
        public virtual extern T Elt { get; set; }
        [Name("next")]
        public virtual extern GenericCell<T> Next { get; set; }
        [Name("new")]
        public virtual extern void New(T elt, GenericCell<T> next);
    }
}

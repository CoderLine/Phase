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
    [Name("haxe.rtti.Rtti")]
    public partial class Rtti
    {
        [Name("getRtti")]
        public static extern Haxe.Rtti.Classdef GetRtti<T>(Haxe.root.Class<T> c);
        [Name("hasRtti")]
        public static extern Haxe.HaxeBool HasRtti<T>(Haxe.root.Class<T> c);
    }
}

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.Unit
{
    [External]
    [Name("haxe.unit.TestResult")]
    public partial class TestResult
    {
        [Name("m_tests")]
        protected virtual extern Haxe.root.List<Haxe.Unit.TestStatus> M_tests { get; set; }
        [Name("success")]
        public virtual extern Haxe.HaxeBool Success { get; private set; }
        [Name("add")]
        public virtual extern void Add(Haxe.Unit.TestStatus t);
        [Name("toString")]
        public virtual extern Haxe.HaxeString ToString();
        [Name("new")]
        public virtual extern void New();
    }
}

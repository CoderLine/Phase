//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.Io
{
    [External]
    [Name("haxe.io.FPHelper")]
    public partial class FPHelper
    {
        [Name("i64tmp")]
        private static extern Haxe.Int64 I64tmp { get; set; }
        [Name("LN2")]
        private static extern Haxe.HaxeFloat LN2 { get; private set; }
        [Name("i32ToFloat")]
        public static extern Haxe.HaxeFloat I32ToFloat(int i);
        [Name("floatToI32")]
        public static extern Haxe.HaxeInt FloatToI32(double f);
        [Name("i64ToDouble")]
        public static extern Haxe.HaxeFloat I64ToDouble(int low, int high);
        [Name("doubleToI64")]
        public static extern Haxe.Int64 DoubleToI64(double v);
    }
}

using Phase.Attributes;

namespace Haxe
{
    [Name("Float")]
    [External]
    public struct HaxeFloat
    {
        public static extern implicit operator double(HaxeFloat d);
        public static extern implicit operator float(HaxeFloat d);
        public static extern implicit operator int(HaxeFloat d);
        public static extern implicit operator HaxeFloat(double d);
        public static extern implicit operator HaxeFloat(float d);
        public static extern implicit operator HaxeFloat(int d);
    }
}

using Phase.Attributes;

namespace Haxe
{
    [Name("Float")]
    [External]
    public class HaxeFloat
    {
        public static extern implicit operator float(HaxeFloat d);
        public static extern implicit operator HaxeFloat(float d);
        public static extern implicit operator int(HaxeFloat d);
        public static extern implicit operator HaxeFloat(int d);
    }
}

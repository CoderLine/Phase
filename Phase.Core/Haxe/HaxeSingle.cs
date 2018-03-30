using Phase.Attributes;

namespace Haxe
{
    [Name("Single")]
    [External]
    public struct HaxeSingle
    {
        public static extern implicit operator double(HaxeSingle d);
        public static extern implicit operator float(HaxeSingle d);
        public static extern implicit operator int(HaxeSingle d);
        public static extern implicit operator HaxeSingle(double d);
        public static extern implicit operator HaxeSingle(float d);
        public static extern implicit operator HaxeSingle(int d);
    }
}
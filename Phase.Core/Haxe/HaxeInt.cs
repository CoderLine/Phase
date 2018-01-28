using Phase.Attributes;

namespace Haxe
{
    [Name("Int")]
    [External]
    public struct HaxeInt
    {
        public static extern implicit operator int(HaxeInt d);
        public static extern implicit operator HaxeInt(int d);
    }
}

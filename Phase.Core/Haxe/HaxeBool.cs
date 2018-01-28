using Phase.Attributes;

namespace Haxe
{
    [Name("Bool")]
    [External]
    public struct HaxeBool
    {
        public static extern implicit operator bool(HaxeBool d);
        public static extern implicit operator HaxeBool(bool d);
    }
}

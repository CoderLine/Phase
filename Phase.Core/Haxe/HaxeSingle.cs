using Phase.Attributes;

namespace Haxe
{
    [Name("Single")]
    [External]
    public class HaxeSingle
    {
        public static extern implicit operator float(HaxeSingle d);
        public static extern implicit operator HaxeSingle(float d);
    }
}

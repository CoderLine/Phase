using Phase.Attributes;

namespace Haxe
{
    [Name("Std")]
    [External]
    public static class Std
    {
        [Name("string")]
        public static extern string String(object o);
        [Name("int")]
        public static extern int Int(float f);
    }
}

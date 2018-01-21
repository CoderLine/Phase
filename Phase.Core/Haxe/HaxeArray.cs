using Phase.Attributes;

namespace Haxe
{
    [External]
    [Name("Array")]
    public class HaxeArray<T>
    {
        public extern HaxeArray();

        public static extern implicit operator T[](HaxeArray<T> d);
        public static extern implicit operator HaxeArray<T>(T[] d);
    }
}
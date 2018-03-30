using Haxe;
using Phase.Attributes;

namespace System
{
    [External]
    public class FixedArray<T>
    {
        public static extern implicit operator T[](FixedArray<T> d);
        public static extern implicit operator FixedArray<T>(T[] d);

        [Name("fromArray")]
        public static extern FixedArray<T> FromArray(HaxeArray<T> array);
    }
}

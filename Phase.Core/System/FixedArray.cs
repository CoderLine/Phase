using Haxe;
using Phase.Attributes;

namespace System
{
    [External]
    public class FixedArray<T>
    {
        [Name("fromArray")]
        public static extern FixedArray<T> FromArray(HaxeArray<T> array);
    }
}

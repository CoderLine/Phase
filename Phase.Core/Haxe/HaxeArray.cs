using System;
using Phase.Attributes;

namespace Haxe
{
    [External]
    [Name("Array")]
    public class HaxeArray<T>
    {
        public extern HaxeArray();

        public static extern implicit operator T[] (HaxeArray<T> d);
        public static extern implicit operator HaxeArray<T>(T[] d);

        [Name("length")]
        public extern HaxeInt Length { get; }

        [NativeIndexer]
        public extern T this[HaxeInt index]
        {
            get;
            set;
        }

        [Name("push")]
        public extern void Push(T item);

        [Name("sort")]
        public extern void Sort(Func<T, T, HaxeInt> comparer);

        [Name("slice")]
        public extern HaxeArray<T> Slice(HaxeInt pos);

        [Name("slice")]
        public extern HaxeArray<T> Slice(HaxeInt pos, HaxeInt end);

        [Name("splice")]
        public extern HaxeArray<T> Splice(HaxeInt index, HaxeInt length);

        [Name("indexOf")]
        public extern int IndexOf(T item);

        [Name("reverse")]
        public extern void Reverse();
    }
}
using Phase.Attributes;

namespace System.Collections.Generic
{
    [External]
    [NativeConstructors]
    public class IterableEnumerable<T> : IEnumerable<T>
    {
        public extern IEnumerator<T> GetEnumerator();

        public extern IterableEnumerable(object withIteratorMethod);

        [External]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

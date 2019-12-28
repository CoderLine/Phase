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
    
    [External]
    [NativeConstructors]
    public class IterableEnumerator<T> : IEnumerator<T>
    {
        public IterableEnumerator()
        {
             
        }
        public extern void Dispose();
        public extern bool MoveNext();
        public extern void Reset();
        public extern T Current { get; }
        object IEnumerator.Current => Current;
    }
}

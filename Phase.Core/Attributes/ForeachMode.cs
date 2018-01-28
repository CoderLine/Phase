namespace Phase.Attributes
{
    public enum ForeachMode
    {
        /// <summary>
        /// The object returned via GetEnumerator will be wrapped into a class that maps the IEnumerator 
        /// into a Iterable
        /// </summary>
        AsIterable,

        /// <summary>
        /// The instance will be directly passed to the foreach loop.
        /// </summary>
        Native, 
        /// <summary>
        /// GetEnumerator will be called on the foreach loop. The GetEnumerator method will automatically
        /// be emitted as Iterable
        /// </summary>
        GetEnumerator,
    }
}
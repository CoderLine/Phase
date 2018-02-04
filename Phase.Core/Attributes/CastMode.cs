namespace Phase.Attributes
{
    public enum CastMode
    {
        /// <summary>
        /// Perform a safe cast to the type cast(Expression, Type)
        /// </summary>
        SafeCast,

        /// <summary>
        /// Perform an unsafe cast cast(Expression).
        /// </summary>
        UnsafeCast, 

        /// <summary>
        /// An untyped keyword is put in front of the expression
        /// </summary>
        Untyped,

        /// <summary>
        /// Do not perform any cast
        /// </summary>
        Ignore
    }
}
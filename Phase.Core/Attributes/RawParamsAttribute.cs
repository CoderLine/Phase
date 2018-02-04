using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    [External]
    public sealed class RawParamsAttribute : Attribute
    {
    }
}
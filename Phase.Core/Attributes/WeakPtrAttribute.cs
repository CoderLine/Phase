using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    [External]
    public sealed class WeakPtrAttribute : Attribute
    {
    }
}
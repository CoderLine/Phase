using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.GenericParameter, Inherited = false)]
    [External]
    public sealed class NullableAttribute : Attribute
    {
    }
}
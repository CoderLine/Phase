using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    [External]
    public sealed class AutoPropertyAttribute : Attribute
    {
    }
}
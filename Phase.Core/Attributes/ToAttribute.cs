using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    [External]
    public sealed class ToAttribute : Attribute
    {
    }
}
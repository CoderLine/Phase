using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    [External]
    public sealed class NoConstructorAttribute : Attribute
    {
    }
}
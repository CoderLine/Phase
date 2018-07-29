using System;
using Phase.Attributes;

namespace Phase.Core.Attributes
{
    [AttributeUsage(AttributeTargets.GenericParameter)]
    [External]
    public sealed class PrimitiveOverloadsAttribute : Attribute
    {
    }
}

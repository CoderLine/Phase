using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    [External]
    public sealed class ExternalAttribute : Attribute
    {
    }
}
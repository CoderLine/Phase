using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Struct, Inherited = false)]
    [External]
    public sealed class OmitImportAttribute : Attribute
    {
    }
}
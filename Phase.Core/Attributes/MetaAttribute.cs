using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [External]
    public sealed class MetaAttribute : Attribute
    {
        public string Meta { get; }

        public MetaAttribute(string meta)
        {
            Meta = meta;
        }
    }
}
using System;
using Phase.Attributes;

namespace Phase.CompilerServices
{
    [External]
    public interface IAttributesBuilder
    {
        void Add(params Attribute[] attributes);
    }
}
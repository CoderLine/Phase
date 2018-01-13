using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    [External]
    public sealed class AbstractAttribute : Attribute
    {
        public string UnterlyingType { get; }
        public string From { get; }
        public string To { get;  }

        public AbstractAttribute(string unterlyingType)
        {
            UnterlyingType = unterlyingType;
        }

        public AbstractAttribute(string unterlyingType, string from, string to)
        {
            UnterlyingType = unterlyingType;
            From = from;
            To = to;
        }
    }
}

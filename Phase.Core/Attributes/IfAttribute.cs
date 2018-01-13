using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    [External]
    public sealed class IfAttribute : Attribute
    {
        public string Condition { get; }

        public IfAttribute(string condition)
        {
            Condition = condition;
        }
    }
}
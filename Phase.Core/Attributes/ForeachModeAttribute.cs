using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [External]
    public sealed class ForeachModeAttribute : Attribute
    {
        public ForeachMode Mode { get; }

        public ForeachModeAttribute(ForeachMode mode)
        {
            Mode = mode;
        }
    }
}
using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [External]
    public sealed class CastModeAttribute : Attribute
    {
        public CastMode Mode { get; }

        public CastModeAttribute(CastMode mode)
        {
            Mode = mode;
        }
    }
}
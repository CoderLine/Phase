using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [External]
    public sealed class NativeAttribute : Attribute
    {
        public string NativeType { get; }

        public NativeAttribute(string native)
        {
            NativeType = native;
        }
    }
}
using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct| AttributeTargets.Delegate | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
    [External]
    public sealed class NameAttribute : Attribute
    {
        public string Name { get; }
        public bool KeepNamespace { get; }

        public NameAttribute(string name, bool keepNamespace = false)
        {
            Name = name;
            KeepNamespace = keepNamespace;
        }
    }
}
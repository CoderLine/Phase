using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
    public sealed class RedirectMethodsToAttribute : Attribute
    {
        public string TypeName { get; }

        public RedirectMethodsToAttribute(string typeName)
        {
            TypeName = typeName;
        }
    }
}

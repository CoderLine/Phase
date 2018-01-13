using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    [External]
    public sealed class OpAttribute : Attribute
    {
        public string Op { get; }

        public OpAttribute(string op)
        {
            Op = op;
        }
    }
}
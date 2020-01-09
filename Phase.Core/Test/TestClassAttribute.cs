using System;

namespace Phase.Test
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class TestClassAttribute : Attribute
    {
    }
    
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class TestMethodAttribute : Attribute
    {
    }
    
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class AsyncTestMethodAttribute : TestMethodAttribute
    {
    }
    
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class IgnoreAttribute : Attribute
    {
    }
}
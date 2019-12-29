using System;

namespace Phase.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = true)]
    public sealed class TemplateAttribute : Attribute
    {
        public string Template { get; }
        public bool SkipSemicolonOnStatements { get; set; }

        public TemplateAttribute(string template)
        {
            Template = template;
        }
    }
}

using System;
using Phase.Attributes;
using Phase.CompilerServices;

namespace Phase
{
    [External]
    public class MsCorlibKotlinCompilerExtension : IKotlinCompilerExtension
    {
        public void Run(ICompilerContext context)
        {
            context.Attributes.Member((string s) => s.Length, AttributeTarget.Getter)
                .Add(new TemplateAttribute("{this}!!.length"));
            context.Attributes.Member((string s) => s[0], AttributeTarget.Getter)
                .Add(new TemplateAttribute("{this}!![{index}]"));
            context.Attributes.Member((string s) => s.Substring(0))
                .Add(new TemplateAttribute("{this}!!.substring({startIndex})"));
            context.Attributes.Member((string s) => s.Replace("", ""))
                .Add(new TemplateAttribute("{this}!!.replace({oldValue}, {newValue})"));
            context.Attributes.Member((string s) => s.ToLower())
                .Add(new TemplateAttribute("{this}!!.toLowerCase()"));
            context.Attributes.Member((string s) => s.ToLowerInvariant())
                .Add(new TemplateAttribute("{this}!!.toLowerCase()"));
            context.Attributes.Member((string s) => s.ToUpper())
                .Add(new TemplateAttribute("{this}!!.toUpperCase()"));
            context.Attributes.Member((string s) => s.ToUpperInvariant())
                .Add(new TemplateAttribute("{this}!!.toUpperCase()"));
            context.Attributes.Member((string s) => s.Trim())
                .Add(new TemplateAttribute("{this}!!.trim()"));
            context.Attributes.Member((string s) => s.StartsWith(""))
                .Add(new TemplateAttribute("{this}!!.startsWith({value})"));
            context.Attributes.Member((string s) => s.EndsWith(""))
                .Add(new TemplateAttribute("{this}!!.endsWith({value})"));
            context.Attributes.Member((string s) => s.IndexOf('0'))
                .Add(new TemplateAttribute("{this}!!.indexOf({value})"));
            context.Attributes.Member((string s) => s.Contains(""))
                .Add(new TemplateAttribute("{this}!!.contains({value})"));
            context.Attributes.Member((object s) => s.GetHashCode())
                .Add(new NameAttribute("hashCode"));
            context.Attributes.Type<string>().Add(new RedirectMethodsToAttribute("system.StringExtensions"));
            context.Attributes.Member(()=>string.Empty).Add(new TemplateAttribute("\"\""));
            context.Attributes.Type<byte>().Add(new RedirectMethodsToAttribute("system.ByteExtensions"));
            context.Attributes.Type<short>().Add(new RedirectMethodsToAttribute("system.ShortExtensions"));
            context.Attributes.Type<int>().Add(new RedirectMethodsToAttribute("system.IntegerExtensions"));
            context.Attributes.Type<long>().Add(new RedirectMethodsToAttribute("system.LongExtensions"));
            context.Attributes.Type<sbyte>().Add(new RedirectMethodsToAttribute("system.SByteExtensions"));
            context.Attributes.Type<ushort>().Add(new RedirectMethodsToAttribute("system.ShortExtensions"));
            context.Attributes.Type<uint>().Add(new RedirectMethodsToAttribute("system.IntegerExtensions"));
            context.Attributes.Type<ulong>().Add(new RedirectMethodsToAttribute("system.LongExtensions"));
            context.Attributes.Type<float>().Add(new RedirectMethodsToAttribute("system.FloatExtensions"));
            context.Attributes.Type<double>().Add(new RedirectMethodsToAttribute("system.DoubleExtensions"));
            context.Attributes.Type<object>().Add(new RedirectMethodsToAttribute("system.ObjectExtensions"));
            context.Attributes.Member((Array x) => x.Length, AttributeTarget.Getter)
                .Add(new TemplateAttribute("{this}!!.size"));
        }
    }
}

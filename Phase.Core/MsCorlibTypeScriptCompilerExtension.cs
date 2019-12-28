using System;
using System.Reflection;
using Phase.Attributes;
using Phase.CompilerServices;

namespace Phase
{
    [External]
    public class MsCorlibTypeScriptCompilerExtension : ITypeScriptCompilerExtension
    {
        public void Run(ICompilerContext context)
        {
            context.Attributes.Type<object>()
                .Add(new RedirectMethodsToAttribute("phase.ObjectExtensions"));
            context.Attributes.Type<Type>()
                .Add(new NameAttribute("CsType"));
            context.Attributes.Type<string>()
                .Add(new NameAttribute("string"), new RedirectMethodsToAttribute("phase.StringExtensions"));
            context.Attributes.Member((string s) => s[0])
                .Add(new TemplateAttribute("{this}.charCodeAt({index})"));
            context.Attributes.Member((string s) => s.Substring(0,1))
                .Add(new TemplateAttribute("{this}.substr({startIndex}, {length})"));
            context.Attributes.Member((string s) => s.Substring(0))
                .Add(new TemplateAttribute("{this}.substr({startIndex})"));
            context.Attributes.Member((string s) => s.IndexOf('c'))
                .Add(new TemplateAttribute("{this}.indexOf({value})"));
            context.Attributes.Member((string s) => s.Replace("a", "b"))
                .Add(new TemplateAttribute("{this}.replace({oldValue}, {newValue})"));
            context.Attributes.Member((string s) => s.ToLower())
                .Add(new TemplateAttribute("{this}.toLowerCase()"));
            context.Attributes.Member((string s) => s.StartsWith(""))
                .Add(new TemplateAttribute("{this}.startsWith({value})"));
            context.Attributes.Member((string s) => s.EndsWith(""))
                .Add(new TemplateAttribute("{this}.endsWith({value})"));
            context.Attributes.Member((string s) => s.LastIndexOf(""))
                .Add(new TemplateAttribute("{this}.lastIndexOf({value})"));
            context.Attributes.Type<bool>()
                .Add(new NameAttribute("boolean"), new RedirectMethodsToAttribute("phase.BooleanExtensions"));
            context.Attributes.Type<byte>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("phase.Uint8Extensions"));
            context.Attributes.Type<char>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("phase.CharExtensions"));
            context.Attributes.Type<short>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("phase.Int16Extensions"));
            context.Attributes.Type<int>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("phase.Int32Extensions"));
            context.Attributes.Type<long>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("phase.Int64Extensions"));
            context.Attributes.Type<sbyte>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("phase.Int8Extensions"));
            context.Attributes.Type<ushort>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("phase.Uint16Extensions"));
            context.Attributes.Type<uint>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("phase.Uint32Extensions"));
            context.Attributes.Type<ulong>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("phase.Uint64Extensions"));
            context.Attributes.Type(typeof(Math))
                .Add(new NameAttribute("CsMath"));
            context.Attributes.Type<float>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("phase.Float32Extensions"));
            context.Attributes.Type<double>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("phase.Float64Extensions"));
        }
    }
}

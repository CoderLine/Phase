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
                .Add(new NameAttribute("unknown"), new RedirectMethodsToAttribute("ph.ObjectExtensions"));
            context.Attributes.Member(() => new object().Equals(2))
                .Add(new NameAttribute("equals"));
            context.Attributes.Type<Type>()
                .Add(new NameAttribute("CsType"));
            context.Attributes.Type<string>()
                .Add(new NameAttribute("string"), new RedirectMethodsToAttribute("ph.StringExtensions"));
            context.Attributes.Member(() => float.IsNaN(0.0f))
                .Add(new TemplateAttribute("isNaN({f})"));
            context.Attributes.Member(() => double.IsNaN(0.0))
                .Add(new TemplateAttribute("isNaN({d})"));
            context.Attributes.Member(() => string.Format("{0}", 1))
                .Add(new NameAttribute("format"));
            context.Attributes.Member(() => string.Format("{0}{1}", 1, 2))
                .Add(new NameAttribute("format"));
            context.Attributes.Member(() => string.Format("{0}{1}{2}", 1, 2, 3))
                .Add(new NameAttribute("format"));
            context.Attributes.Member(() => string.Format("{0}{1}{2}{3}", new object[]{1, 2, 3, 4}))
                .Add(new NameAttribute("format"));
            context.Attributes.Member((string s) => s[0])
                .Add(new TemplateAttribute("{this}.charCodeAt({index})"));
            context.Attributes.Member((string s) => s.Substring(0,1))
                .Add(new TemplateAttribute("{this}.substr({startIndex}, {length})"));
            context.Attributes.Member((string s) => s.Substring(0))
                .Add(new TemplateAttribute("{this}.substr({startIndex})"));
            context.Attributes.Member((string s) => s.IndexOf('c'))
                .Add(new TemplateAttribute("{this}.indexOf(String.fromCharCode({value}))"));
            context.Attributes.Member((string s) => s.Replace("a", "b"))
                .Add(new TemplateAttribute("{this}.split({oldValue}).join({newValue})"));
            context.Attributes.Member((string s) => s.ToLower())
                .Add(new TemplateAttribute("{this}.toLowerCase()"));
            context.Attributes.Member((string s) => s.ToUpper())
                .Add(new TemplateAttribute("{this}.toUpperCase()"));
            context.Attributes.Member((string s) => s.StartsWith(""))
                .Add(new TemplateAttribute("{this}.startsWith({value})"));
            context.Attributes.Member((string s) => s.Trim())
                .Add(new TemplateAttribute("{this}.trim()"));
            context.Attributes.Member((string s) => s.EndsWith(""))
                .Add(new TemplateAttribute("{this}.endsWith({value})"));
            context.Attributes.Member((string s) => s.IndexOf(""))
                .Add(new TemplateAttribute("{this}.indexOf({value})"));
            context.Attributes.Member((string s) => s.LastIndexOf(""))
                .Add(new TemplateAttribute("{this}.lastIndexOf({value})"));
            context.Attributes.Member((string s) => s.LastIndexOf('c'))
                .Add(new TemplateAttribute("{this}.lastIndexOf(String.fromCharCode({value}))"));
            context.Attributes.Type<bool>()
                .Add(new NameAttribute("boolean"), new RedirectMethodsToAttribute("ph.BooleanExtensions"));
            context.Attributes.Type<byte>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("ph.Uint8Extensions"));
            context.Attributes.Type<char>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("ph.CharExtensions"));
            context.Attributes.Type<short>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("ph.Int16Extensions"));
            context.Attributes.Type<int>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("ph.Int32Extensions"));
            context.Attributes.Type<long>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("ph.Int64Extensions"));
            context.Attributes.Type<sbyte>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("ph.Int8Extensions"));
            context.Attributes.Type<ushort>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("ph.Uint16Extensions"));
            context.Attributes.Type<uint>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("ph.Uint32Extensions"));
            context.Attributes.Type<ulong>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("ph.Uint64Extensions"));
            context.Attributes.Type(typeof(Math))
                .Add(new NameAttribute("system.CsMath"));
            context.Attributes.Member(()=> 0.CompareTo(0))
                .Add(new TemplateAttribute("({this} - {value})"));
            context.Attributes.Member(()=> 0.0.CompareTo(0.0))
                .Add(new TemplateAttribute("({this} - {value})"));
            context.Attributes.Member(()=> Math.PI)
                .Add(new TemplateAttribute("Math.PI"));
            context.Attributes.Member(()=> Math.Abs(0.0))
                .Add(new TemplateAttribute("Math.abs({value})"));
            context.Attributes.Member(()=> Math.Abs(0.0f))
                .Add(new TemplateAttribute("Math.abs({value})"));
            context.Attributes.Member(()=> Math.Abs(0))
                .Add(new TemplateAttribute("Math.abs({value})"));
            context.Attributes.Member(()=> Math.Min(0,0))
                .Add(new TemplateAttribute("Math.min({val1}, {val2})"));
            context.Attributes.Member(()=> Math.Min(0.0f,0.0f))
                .Add(new TemplateAttribute("Math.min({val1}, {val2})"));
            context.Attributes.Member(()=> Math.Min(0.0,0.0))
                .Add(new TemplateAttribute("Math.min({val1}, {val2})"));
            context.Attributes.Member(()=> Math.Max(0,0))
                .Add(new TemplateAttribute("Math.max({val1}, {val2})"));
            context.Attributes.Member(()=> Math.Max(0.0f,0.0f))
                .Add(new TemplateAttribute("Math.max({val1}, {val2})"));
            context.Attributes.Member(()=> Math.Max(0.0,0.0))
                .Add(new TemplateAttribute("Math.max({val1}, {val2})"));
            context.Attributes.Member(()=> Math.Pow(0,0))
                .Add(new TemplateAttribute("Math.pow({x}, {y})"));
            context.Attributes.Member(()=> Math.Log10(0))
                .Add(new TemplateAttribute("Math.log10({d})"));
            context.Attributes.Member(()=> Math.Sqrt(0))
                .Add(new TemplateAttribute("Math.sqrt({d})"));
            context.Attributes.Member(()=> Math.Exp(0))
                .Add(new TemplateAttribute("Math.exp({d})"));
            context.Attributes.Member(()=> Math.Log(0))
                .Add(new TemplateAttribute("Math.log({d})"));
            context.Attributes.Member(()=> Math.Sin(0))
                .Add(new TemplateAttribute("Math.sin({a})"));
            context.Attributes.Member(()=> Math.Asin(0))
                .Add(new TemplateAttribute("Math.asin({d})"));
            context.Attributes.Member(()=> Math.Cos(0))
                .Add(new TemplateAttribute("Math.cos({a})"));
            context.Attributes.Member(()=> Math.Tan(0))
                .Add(new TemplateAttribute("Math.tan({a})"));
            context.Attributes.Member(()=> Math.Round(0.0))
                .Add(new TemplateAttribute("Math.round({a})"));
            context.Attributes.Member(()=> Math.Ceiling(0.0))
                .Add(new TemplateAttribute("Math.ceil({a})"));
            context.Attributes.Member(()=> Math.Floor(0.0))
                .Add(new TemplateAttribute("Math.floor({d})"));
            context.Attributes.Type<float>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("ph.Float32Extensions"));
            context.Attributes.Type<double>()
                .Add(new NameAttribute("number"), new RedirectMethodsToAttribute("ph.Float64Extensions"));
        }
    }
}

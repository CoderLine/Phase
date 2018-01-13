using System;
using Phase.Attributes;
using Phase.CompilerServices;

namespace Phase
{
    [External]
    public class MsCorlibCompilerExtension : ICompilerExtension
    {
        public void Run(ICompilerContext context)
        {
            context.Attributes.Type<string>()
                .Add(new NameAttribute("system.CsString"));
            context.Attributes.Type<bool>()
                .Add(new NameAttribute("system.Boolean"));
            context.Attributes.Type<byte>()
                .Add(new NameAttribute("system.Byte"));
            context.Attributes.Type<char>()
                .Add(new NameAttribute("system.Char"));
            context.Attributes.Type<short>()
                .Add(new NameAttribute("system.Int16"));
            context.Attributes.Type<int>()
                .Add(new NameAttribute("system.Int32"));
            context.Attributes.Type<long>()
                .Add(new NameAttribute("system.Int64"));
            context.Attributes.Type<sbyte>()
                .Add(new NameAttribute("system.SByte"));
            context.Attributes.Type<ushort>()
                .Add(new NameAttribute("system.UInt16"));
            context.Attributes.Type<uint>()
                .Add(new NameAttribute("system.UInt32"));
            context.Attributes.Type<ulong>()
                .Add(new NameAttribute("system.UInt64"));
            context.Attributes.Type(typeof(Math))
                .Add(new NameAttribute("system.CsMath"));
            context.Attributes.Type<float>()
                .Add(new NameAttribute("system.Single"));
            context.Attributes.Type<double>()
                .Add(new NameAttribute("system.Double"));
        }
    }
}

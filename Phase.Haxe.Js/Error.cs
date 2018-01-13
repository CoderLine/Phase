using System;
using Phase.Attributes;

namespace Haxe.Js
{
    [NativeConstructors]
    [External]
    [Name("js.Error")]
    public class Error : Exception
    {
        [Name("stack")]
        public extern string Stack { get;}
    }
}

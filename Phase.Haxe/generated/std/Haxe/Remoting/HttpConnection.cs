//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.Remoting
{
    [External]
    [Name("haxe.remoting.HttpConnection")]
    public partial class HttpConnection
        : Haxe.Remoting.Connection
    {
        [Name("__url")]
        protected virtual extern Haxe.HaxeString __url { get; set; }
        [Name("__path")]
        protected virtual extern Haxe.HaxeArray<Haxe.HaxeString> __path { get; set; }
        [Name("resolve")]
        public virtual extern Haxe.Remoting.Connection Resolve(string name);
        [Name("call")]
        public virtual extern dynamic Call(Haxe.HaxeArray<object> @params);
        [Name("new")]
        protected virtual extern void New(string url, Haxe.HaxeArray<string> path);
        [Name("TIMEOUT")]
        public static extern Haxe.HaxeFloat TIMEOUT { get; set; }
        [Name("urlConnect")]
        public static extern HttpConnection UrlConnect(string url);
        [Name("processRequest")]
        public static extern Haxe.HaxeString ProcessRequest(string requestData, Haxe.Remoting.Context ctx);
    }
}

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
    [Name("haxe.remoting.DelayedConnection")]
    public partial class DelayedConnection
        : Haxe.Remoting.AsyncConnection
    {
        [Name("connection")]
        public virtual extern Haxe.Remoting.AsyncConnection Connection { get; set; }
        [Name("__path")]
        protected virtual extern Haxe.HaxeArray<Haxe.HaxeString> __path { get; set; }
        [Name("__data")]
        protected virtual extern dynamic __data { get; set; }
        [Name("setErrorHandler")]
        public virtual extern void SetErrorHandler(Action<object> h);
        [Name("resolve")]
        public virtual extern Haxe.Remoting.AsyncConnection Resolve(string name);
        [Name("get_connection")]
        protected virtual extern Haxe.Remoting.AsyncConnection Get_connection();
        [Name("set_connection")]
        protected virtual extern Haxe.Remoting.AsyncConnection Set_connection(Haxe.Remoting.AsyncConnection cnx);
        [Name("call")]
        public virtual extern void Call(Haxe.HaxeArray<object> @params, Action<object> onResult = default(Action<object>));
        [Name("new")]
        protected virtual extern void New(dynamic data, Haxe.HaxeArray<string> path);
        [Name("process")]
        private static extern void Process(DelayedConnection d);
        [Name("create")]
        public static extern DelayedConnection Create();
    }
}
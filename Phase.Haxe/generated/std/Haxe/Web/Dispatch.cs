//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.Web
{
    [External]
    [Name("haxe.web.Dispatch")]
    public partial class Dispatch
    {
        [Name("parts")]
        public virtual extern Haxe.HaxeArray<Haxe.HaxeString> Parts { get; set; }
        [Name("params")]
        public virtual extern Haxe.root.Map<Haxe.HaxeString, Haxe.HaxeString> Params { get; set; }
        [Name("name")]
        public virtual extern Haxe.HaxeString Name { get; set; }
        [Name("cfg")]
        public virtual extern Haxe.Web.DispatchConfig Cfg { get; set; }
        [Name("subDispatch")]
        protected virtual extern Haxe.HaxeBool SubDispatch { get; set; }
        [Name("dispatch")]
        public virtual extern void _Dispatch(dynamic obj);
        [Name("getParams")]
        public virtual extern dynamic GetParams();
        [Name("onMeta")]
        public virtual extern void OnMeta(string v, Haxe.root.Null<Haxe.HaxeArray<object>> args);
        [Name("resolveName")]
        protected virtual extern Haxe.HaxeString ResolveName(string name);
        [Name("runtimeDispatch")]
        public virtual extern void RuntimeDispatch(Haxe.Web.DispatchConfig cfg);
        [Name("redirect")]
        public virtual extern void Redirect(string url, Haxe.root.Map<string, string> @params = default(Haxe.root.Map<string, string>));
        [Name("runtimeGetParams")]
        public virtual extern dynamic RuntimeGetParams(int cfgIndex);
        [Name("match")]
        protected virtual extern dynamic Match(string v, Haxe.Web.MatchRule r, bool opt);
        [Name("checkParams")]
        protected virtual extern dynamic CheckParams(Haxe.HaxeArray<dynamic> @params, bool opt);
        [Name("loop")]
        protected virtual extern void Loop(Haxe.HaxeArray<object> args, Haxe.Web.DispatchRule r);
        [Name("new")]
        public virtual extern void New(string url, Haxe.root.Map<string, string> @params);
        [Name("GET_RULES")]
        private static extern Haxe.HaxeArray<Haxe.HaxeArray<dynamic>> GET_RULES { get; set; }
        [Name("make")]
        public static extern Haxe.Web.DispatchConfig Make(dynamic obj);
        [Name("run")]
        public static extern void Run(string url, Haxe.root.Map<string, string> @params, dynamic obj);
        [Name("extractConfig")]
        private static extern Haxe.Web.DispatchConfig ExtractConfig(object obj);
    }
}

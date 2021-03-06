//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.root
{
    [External]
    [Name("StringTools")]
    public partial class StringTools
    {
        [Name("urlEncode")]
        public static extern Haxe.HaxeString UrlEncode(string s);
        [Name("urlDecode")]
        public static extern Haxe.HaxeString UrlDecode(string s);
        [Name("htmlEscape")]
        public static extern Haxe.HaxeString HtmlEscape(string s, bool quotes = default(bool));
        [Name("htmlUnescape")]
        public static extern Haxe.HaxeString HtmlUnescape(string s);
        [Name("startsWith")]
        public static extern Haxe.HaxeBool StartsWith(string s, string start);
        [Name("endsWith")]
        public static extern Haxe.HaxeBool EndsWith(string s, string end);
        [Name("isSpace")]
        public static extern Haxe.HaxeBool IsSpace(string s, int pos);
        [Name("ltrim")]
        public static extern Haxe.HaxeString Ltrim(string s);
        [Name("rtrim")]
        public static extern Haxe.HaxeString Rtrim(string s);
        [Name("trim")]
        public static extern Haxe.HaxeString Trim(string s);
        [Name("lpad")]
        public static extern Haxe.HaxeString Lpad(string s, string c, int l);
        [Name("rpad")]
        public static extern Haxe.HaxeString Rpad(string s, string c, int l);
        [Name("replace")]
        public static extern Haxe.HaxeString Replace(string s, string sub, string by);
        [Name("hex")]
        public static extern Haxe.HaxeString Hex(int n, int digits = default(int));
        [Name("fastCodeAt")]
        public static extern Haxe.HaxeInt FastCodeAt(string s, int index);
        [Name("isEof")]
        public static extern Haxe.HaxeBool IsEof(int c);
        [Name("quoteUnixArg")]
        public static extern Haxe.HaxeString QuoteUnixArg(string argument);
        [Name("winMetaCharacters")]
        public static extern Haxe.HaxeArray<Haxe.HaxeInt> WinMetaCharacters { get; set; }
        [Name("quoteWinArg")]
        public static extern Haxe.HaxeString QuoteWinArg(string argument, bool escapeMetaCharacters);
    }
}

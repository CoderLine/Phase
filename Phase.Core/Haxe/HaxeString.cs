using Phase.Attributes;

namespace Haxe
{
    [Name("String")]
    [External]
    public class HaxeString
    {
        public static extern implicit operator string(HaxeString d);
        public static extern implicit operator HaxeString(string d);

        public static extern string operator +(HaxeString l, object r);
        public static extern string operator +(HaxeString l, HaxeString r);

        public static extern bool operator ==(HaxeString l, object r);
        public static extern bool operator !=(HaxeString l, object r);
        public static extern string operator +(object l, HaxeString r);
        public static extern bool operator ==(object l, HaxeString r);
        public static extern bool operator !=(object l, HaxeString r);

        public static extern bool operator ==(HaxeString l, HaxeString r);
        public static extern bool operator !=(HaxeString l, HaxeString r);

        public override extern bool Equals(object obj);
        public override extern int GetHashCode();

        [Name("length")]
        public extern HaxeInt Length { get; }

        [Name("charAt")] public extern HaxeString CharAt(HaxeInt index);
        [Name("charCodeAt")] public extern HaxeInt? CharCodeAt(HaxeInt index);
        [Name("indexOf")] public extern HaxeInt IndexOf(HaxeString str);
        [Name("indexOf")] public extern HaxeInt IndexOf(HaxeString str, HaxeInt startIndex);
        [Name("lastIndexOf")] public extern HaxeInt LastIndexOf(HaxeString str);
        [Name("lastIndexOf")] public extern HaxeInt LastIndexOf(HaxeString str, HaxeInt startIndex);
        [Name("split")] public extern HaxeArray<HaxeString> Split(HaxeString delimiter);
        [Name("substr")] public extern HaxeArray<HaxeString> SubStr(HaxeInt pos);
        [Name("substr")] public extern HaxeArray<HaxeString> SubStr(HaxeInt pos, HaxeInt len);
        [Name("substring")] public extern HaxeArray<HaxeString> Substring(HaxeInt startIndex);
        [Name("substring")] public extern HaxeArray<HaxeString> Substring(HaxeInt startIndex, HaxeInt endIndex);
        [Name("toLowerCase")] public extern HaxeString ToLowerCase();
        [Name("toString")] public new extern HaxeString ToString();
        [Name("toUpperCase")] public extern HaxeString ToUpperCase();

        [Name("fromCharCode")] public static extern HaxeString FromCharCode(HaxeInt code);
    }
}

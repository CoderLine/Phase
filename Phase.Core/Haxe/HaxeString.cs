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
    }
}

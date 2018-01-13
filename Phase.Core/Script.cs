using Phase.Attributes;

namespace Phase
{
    [External]
    public static class Script
    {
        public static extern void Write(string code);
        public static extern T Write<T>(string code);
        public static extern T As<T>(this object v);
    }
}

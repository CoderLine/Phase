using Phase.Attributes;

namespace Phase
{
    [External]
    public static class Script
    {
        [Template("{code:raw}", SkipSemicolonOnStatements = true)]
        public static extern void Write(string code);
        [Template("{code:raw}", SkipSemicolonOnStatements = true)]
        public static extern T Write<T>(string code);
        [Template("{v} as {T}")]
        public static extern T As<T>(this object v);
        public static extern object AbstractThis
        {
            [Template("this")]
            get;
            [Template("this = {value}")]
            set;
        }
        [Template("this")]
        public static extern T This<T>();
    }
}

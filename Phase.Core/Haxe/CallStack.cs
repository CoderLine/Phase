using Phase.Attributes;

namespace Haxe
{
    [External]
    [Name("haxe.StackItem")]
    public class StackItem
    {
        private extern StackItem();
    }

    [External]
    [Name("haxe.CallStack")]
    public static class CallStack
    {
        [Name("callStack")]
        public static extern HaxeArray<StackItem> Get();

        [Name("ExceptionStack")]
        public static extern HaxeArray<StackItem> ExceptionStack();

        [Name("toString")]
        public static extern string ToString(HaxeArray<StackItem> stack);
    }
}

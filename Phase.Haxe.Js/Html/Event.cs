using Phase.Attributes;

namespace Haxe.Js.Html
{
    [External]
    [Name("js.html.Event")]
    public class Event
    {
        public static readonly int NONE;
        public static readonly int CAPTURING_PHASE;
        public static readonly int AT_TARGET;
        public static readonly int BUBBLING_PHASE;
        public static readonly int ALT_MASK;
        public static readonly int CONTROL_MASK;
        public static readonly int SHIFT_MASK;
        public static readonly int META_MASK;

        [Name("type")] public extern string Type { get; }
        [Name("target")] public extern EventTarget Target { get; }
        [Name("currentTarget")] public extern EventTarget CurrentTarget { get; }
        [Name("eventPhase")] public extern int EventPhase { get; }
        [Name("bubbles")] public extern bool Bubbles { get; }
        [Name("cancelable")] public extern bool Cancelable { get; }
        [Name("defaultPrevented")] public extern bool DefaultPrevented { get; }
        [Name("isTrusted")] public extern bool IsTrusted { get; }
        [Name("timeStamp")] public extern float TimeStamp { get; }
        [Name("originalTarget")] public extern EventTarget OriginalTarget { get; }
        [Name("explicitOriginalTarget")] public extern EventTarget ExplicitOriginalTarget { get; }

        public extern Event(string type, dynamic eventInitDict);

        [Name("stopPropagation")] public extern void StopPropagation();
        [Name("stopImmediatePropagation")] public extern void StopImmediatePropagation();
        [Name("preventDefault")] public extern void PreventDefault();
        [Name("initEvent")] public extern void InitEvent(string type, bool bubbles, bool cancelable);
        [Name("getPreventDefault")] public extern bool GetPreventDefault();
    }
}
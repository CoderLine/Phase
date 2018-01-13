namespace Phase.CompilerServices
{
    [Attributes.External]
    public enum AttributeTarget
    {
        Default,
        ReturnValue,
        Parameter,
        Getter,
        Setter,
        Adder,
        Remover
    }
}
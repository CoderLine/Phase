using Phase.Attributes;

namespace Phase.CompilerServices
{
    [External]
    public interface ICompilerContext
    {
        IAttributesContext Attributes { get; }
    }
}
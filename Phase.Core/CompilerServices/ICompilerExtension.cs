namespace Phase.CompilerServices
{
    public interface ICompilerExtension
    {
        void Run(ICompilerContext context);
    }
    public interface IHaxeCompilerExtension : ICompilerExtension
    {
    }
    public interface ICppHaxeCompilerExtension : ICompilerExtension
    {
    }
}

using Microsoft.CodeAnalysis;

namespace Phase.Translator
{
    public static class PhaseErrors
    {
        public static readonly DiagnosticDescriptor PH001 = new DiagnosticDescriptor("PH001",
            "[CompilerExtension] methods must accept exactly one parameter of type Phase.CompilerServices.ICompilerContext",
            "Change the method parameters of '{0}' to a single Phase.CompilerServices.ICompilerContext",
            "Phase.CompilerExtension", DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor PH002 = new DiagnosticDescriptor("PH002",
            "[CompilerExtension] methods must be static",
            "Add the static keyword to '{0}'",
            "Phase.CompilerExtension", DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor PH003 = new DiagnosticDescriptor("PH003",
            "[CompilerExtension] methods must declare a body",
            "Add a method body to '{0}'",
            "Phase.CompilerExtension", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor PH004 = new DiagnosticDescriptor("PH004",
            "All parameters must be compile time constant",
            "The parameter '{0}' must be compile time constant",
            "Phase.CompilerExtension", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor PH005 = new DiagnosticDescriptor("PH005",
            "Could not resolve assembly",
            "Assembly with name '{0}' could not be found within the references, available assemblies are: '{1}'",
            "Phase.CompilerExtension", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor PH006 = new DiagnosticDescriptor("PH006",
            "Could not resolve member",
            "Please ensure to use a simple lambda expression to access the member",
            "Phase.CompilerExtension", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor PH007 = new DiagnosticDescriptor("PH007",
            "Invalid AttributeTarget for symbol",
            "The attribute target '{0}' is only allowed for '{1}' symbols",
            "Phase.CompilerExtension", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor PH008 = new DiagnosticDescriptor("PH008",
            "Could not find parameter with given name",
            "The method '{0}' did not contain any parameter named '{1}', available parameters are '{2}'",
            "Phase.CompilerExtension", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor PH009 = new DiagnosticDescriptor("PH009",
            "Could not resolve constructor",
            "Please create a new object within the first parameter.",
            "Phase.CompilerExtension", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor PH010 = new DiagnosticDescriptor("PH010",
            "Could not resolve event",
            "Event with name '{0}' could not be found within the type '{1}', available events are: '{2}'",
            "Phase.CompilerExtension", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor PH011 = new DiagnosticDescriptor("PH011",
            "Invalid method invocation, only invocations to ICompilerContext related members is allowed",
            "Invalid method invocation, only invocations to ICompilerContext related members is allowed",
            "Phase.CompilerExtension", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor PH012 = new DiagnosticDescriptor("PH012",
            "Could not resolve details of IAttributeContext on which the method was invoked, please simplify method invocation",
            "Could not resolve details of IAttributeContext on which the method was invoked, please simplify method invocation",
            "Phase.CompilerExtension", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor PH013 = new DiagnosticDescriptor("PH013",
            "Attributes must be directly created via new expression and object initializers",
            "Attributes must be directly created via new expression and object initializers",
            "Phase.CompilerExtension", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor PH014 = new DiagnosticDescriptor("PH014",
            "Unexpected object initializer content, please only use property assignments.",
            "Unexpected object initializer content, please only use property assignments.",
            "Phase.CompilerExtension", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor PH016 = new DiagnosticDescriptor("PH016",
            "This overload of the Type registration requires a direct typeof() expression as parameter.",
            "This overload of the Type registration requires a direct typeof() expression as parameter.",
            "Phase.CompilerExtension", DiagnosticSeverity.Error, true);

    }
}

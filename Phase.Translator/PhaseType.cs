using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator
{
    public enum PhaseTypeKind
    {
        Class,
        Interface,
        Enum,
        Struct,
        Delegate
    }

    public abstract class PhaseType
    {
        public SemanticModel SemanticModel { get; }
        public SyntaxNode RootNode { get; }
        public List<PhaseType> PartialDeclarations { get; }
        public INamedTypeSymbol TypeSymbol { get; }
        public abstract PhaseTypeKind Kind { get; }

        public bool IsNested => TypeSymbol.ContainingType != null;

        protected PhaseType(SyntaxNode rootNode, SemanticModel semanticModel)
        {
            SemanticModel = semanticModel;
            RootNode = rootNode;
            TypeSymbol = semanticModel.GetDeclaredSymbol(RootNode) as INamedTypeSymbol;
            PartialDeclarations = new List<PhaseType>();
            PartialDeclarations.Add(this);
        }

        public void Merge(PhaseType type)
        {
            PartialDeclarations.Add(type);
        }
    }

    public class PhaseClass : PhaseType
    {
        public ClassDeclarationSyntax ClassDeclaration { get; private set; }
        public override PhaseTypeKind Kind => PhaseTypeKind.Class;

        public PhaseClass(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
            : base(classDeclaration, semanticModel)
        {
            ClassDeclaration = classDeclaration;
        }
    }

    public class PhaseInterface : PhaseType
    {
        public InterfaceDeclarationSyntax InterfaceDeclaration { get; private set; }
        public override PhaseTypeKind Kind => PhaseTypeKind.Interface;

        public PhaseInterface(InterfaceDeclarationSyntax interfaceDeclaration, SemanticModel semanticModel)
                  : base(interfaceDeclaration, semanticModel)
        {
            InterfaceDeclaration = interfaceDeclaration;
        }
    }


    public class PhaseEnum : PhaseType
    {
        public EnumDeclarationSyntax EnumDeclaration { get; private set; }
        public override PhaseTypeKind Kind => PhaseTypeKind.Enum;

        public PhaseEnum(EnumDeclarationSyntax enumDeclaration, SemanticModel semanticModel)
                  : base(enumDeclaration, semanticModel)
        {
            EnumDeclaration = enumDeclaration;
        }
    }

    public class PhaseStruct : PhaseType
    {
        public StructDeclarationSyntax StructDeclaration { get; private set; }
        public override PhaseTypeKind Kind => PhaseTypeKind.Struct;

        public PhaseStruct(StructDeclarationSyntax structDeclaration, SemanticModel semanticModel)
                : base(structDeclaration, semanticModel)
        {
            StructDeclaration = structDeclaration;
        }
    }

    public class PhaseDelegate : PhaseType
    {
        public DelegateDeclarationSyntax DelegateDeclaration { get; private set; }
        public override PhaseTypeKind Kind => PhaseTypeKind.Delegate;

        public PhaseDelegate(DelegateDeclarationSyntax delegateDeclaration, SemanticModel semanticModel)
                : base(delegateDeclaration, semanticModel)
        {
            DelegateDeclaration = delegateDeclaration;
        }
    }


}

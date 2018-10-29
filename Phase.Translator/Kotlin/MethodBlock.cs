using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin
{
    public class MethodBlock : AbstractKotlinEmitterBlock
    {
        private readonly IMethodSymbol _method;
        private readonly bool _asExtensionMethod;

        public MethodBlock(KotlinEmitterContext context, IMethodSymbol method, bool asExtensionMethod = false)
            : base(context)
        {
            _method = method;
            _asExtensionMethod = asExtensionMethod;
        }

        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Emitter.IsExternal(_method)
                || Emitter.IsCompilerExtension(_method)
                || (_method.MethodKind == MethodKind.PropertyGet && !((IPropertySymbol)_method.AssociatedSymbol).IsIndexer)
                || (_method.MethodKind == MethodKind.PropertySet && !((IPropertySymbol)_method.AssociatedSymbol).IsIndexer)
            )
            {
                return;
            }

            if (!_method.ExplicitInterfaceImplementations.IsEmpty
                || (_method.MethodKind == MethodKind.PropertyGet && !((IPropertySymbol)_method.AssociatedSymbol).ExplicitInterfaceImplementations.IsEmpty)
                || (_method.MethodKind == MethodKind.PropertySet && !((IPropertySymbol)_method.AssociatedSymbol).ExplicitInterfaceImplementations.IsEmpty)
            )
            {
                return;
            }

            var isReifiedExtensionMethod = Emitter.IsReifiedExtensionMethod(_method);
            if (isReifiedExtensionMethod && !_asExtensionMethod ||
                !isReifiedExtensionMethod && _asExtensionMethod)
            {
                return;
            }

            if (_method.MethodKind == MethodKind.Destructor)
            {
                // TODO: Warning: Destructors not supported
                return;
            }

            if (_method.MethodKind == MethodKind.StaticConstructor && _method.DeclaringSyntaxReferences.Length == 0)
            {
                // implicit static constructor
                return;
            }

            if (_method.ContainingType.TypeKind == TypeKind.Struct && Emitter.IsAbstract(_method.ContainingType) && _method.MethodKind == MethodKind.Constructor && _method.DeclaringSyntaxReferences.Length == 0)
            {
                // implicit constructor for structs
                return;
            }

            WriteComments(_method, cancellationToken);

            if (_method.IsStatic && _method.MethodKind != MethodKind.StaticConstructor)
            {
                Write("@JvmStatic");
                WriteNewLine();
            }

            WriteMeta(_method, cancellationToken);

            if (_method.MethodKind == MethodKind.StaticConstructor)
            {
                // Static constructors are simply blocks in java
            }
            else if (_method.ContainingType.TypeKind == TypeKind.Interface)
            {
                WriteAccessibility(Accessibility.Public);
            }
            else if (_method.MethodKind == MethodKind.PropertyGet || _method.MethodKind == MethodKind.PropertyGet || _method.MethodKind == MethodKind.EventAdd || _method.MethodKind == MethodKind.EventRemove)
            {
                var access = _method.DeclaredAccessibility;
                if (access == Accessibility.NotApplicable)
                {
                    access = _method.AssociatedSymbol.DeclaredAccessibility;
                }
                WriteAccessibility(access);
            }
            else
            {
                WriteAccessibility(_method.DeclaredAccessibility);
            }


            if (isReifiedExtensionMethod)
            {
                Write("inline ");
            }

            if (_method.IsOverride || Emitter.IsInterfaceImplementation(_method))
            {
                Write("override ");
            }
            else if (_method.IsVirtual)
            {
                Write("open ");
            }

            if ((_method.MethodKind == MethodKind.PropertyGet && ((IPropertySymbol)_method.AssociatedSymbol).IsIndexer)
                || (_method.MethodKind == MethodKind.PropertySet && ((IPropertySymbol)_method.AssociatedSymbol).IsIndexer)
            )
            {
                Write("operator ");
            }
            
            switch (_method.MethodKind)
            {
                case MethodKind.StaticConstructor:
                    Write("init ");
                    break;
                case MethodKind.Constructor:
                    break;
                default:
                    if (_method.IsAbstract)
                    {
                        Write("abstract ");
                    }
                    Write("fun ");
                    break;
            }

            var typeParameters = new List<ITypeSymbol>(_method.TypeParameters);
            if (isReifiedExtensionMethod)
            {
                CollectTypeParameters(typeParameters, _method.ContainingType);
            }
            if (_method.IsStatic)
            {
                CollectTypeParameters(typeParameters, _method.ReturnType);
                foreach (var parameter in _method.Parameters)
                {
                    CollectTypeParameters(typeParameters, parameter.Type);
                }
            }


            if (typeParameters.Count > 0)
            {
                Write("<");
                for (int i = 0; i < typeParameters.Count; i++)
                {
                    if (i > 0)
                    {
                        WriteComma();
                    }

                    if (isReifiedExtensionMethod)
                    {
                        Write("reified ");
                    }
                    Write(typeParameters[i].Name);
                }
                Write("> ");
            }

            if (_method.MethodKind != MethodKind.StaticConstructor)
            {
                if (isReifiedExtensionMethod)
                {
                    Write(Emitter.GetTypeName(_method.ContainingType, true, false, false));
                    WriteDot();
                }


                var methodName = Emitter.GetMethodName(_method);
                Write(methodName);
                WriteOpenParentheses();
                WriteParameterDeclarations(_method.Parameters, cancellationToken);
                WriteCloseParentheses();
                WriteSpace();

                switch (_method.MethodKind)
                {
                    case MethodKind.PropertyGet:
                    case MethodKind.PropertySet:
                        Write(" : ");
                        WriteType(((IPropertySymbol)_method.AssociatedSymbol).Type);
                        break;
                    case MethodKind.EventAdd:
                    case MethodKind.EventRemove:
                        break;
                    case MethodKind.Constructor:
                    case MethodKind.StaticConstructor:
                        break;
                    default:
                        var retType = _method.ReturnType;
                        if (retType.SpecialType != SpecialType.System_Void)
                        {
                            Write(" : ");
                            if (Emitter.IsGetEnumeratorAsIterator(_method))
                            {
                                Write("Iterable<");
                                var generic = ((INamedTypeSymbol)_method.ReturnType).TypeArguments[0];
                                WriteType(generic);
                                Write(">");
                            }
                            else
                            {
                                Write(Emitter.GetTypeName(_method.ReturnType, false, false, _method.Name != "ToString"));
                            }
                        }
                        break;
                }

                Write(" ");

            }

            if (_method.ContainingType.TypeKind == TypeKind.Interface || (Emitter.IsNative(_method.ContainingType) && _method.IsExtern) || _method.IsAbstract)
            {
                WriteNewLine();
            }
            else
            {
                if (!_method.DeclaringSyntaxReferences.IsEmpty)
                {
                    EmitterContext.ParameterNames.Clear();
                    foreach (var reference in _method.DeclaringSyntaxReferences)
                    {
                        var node = reference.GetSyntax(cancellationToken);
                        var methodDeclarationSyntax = node as MethodDeclarationSyntax;
                        var constructorDeclarationSyntax = node as ConstructorDeclarationSyntax;
                        var accessorDeclarationSyntax = node as AccessorDeclarationSyntax;
                        var operatorDeclarationSyntax = node as OperatorDeclarationSyntax;
                        var conversionOperatorDeclarationSyntax = node as ConversionOperatorDeclarationSyntax;
                        var arrowExpressionClauseSyntax = node as ArrowExpressionClauseSyntax;
                        if (methodDeclarationSyntax != null)
                        {
                            WriteNewLine();
                            BeginBlock();

                            if (methodDeclarationSyntax.ExpressionBody != null)
                            {
                                if (!_method.ReturnsVoid)
                                {
                                    WriteReturn(true);
                                }
                                EmitTree(methodDeclarationSyntax.ExpressionBody.Expression,
                                    cancellationToken);
                                WriteSemiColon(true);
                            }
                            else if (methodDeclarationSyntax.Body != null)
                            {
                                WriteLocalParameters(methodDeclarationSyntax.Body, cancellationToken);
                                foreach (var statement in methodDeclarationSyntax.Body.Statements)
                                {
                                    EmitTree(statement, cancellationToken);
                                }
                            }
                        }
                        else if (conversionOperatorDeclarationSyntax != null)
                        {
                            WriteNewLine();
                            BeginBlock();

                            if (conversionOperatorDeclarationSyntax.ExpressionBody != null)
                            {
                                if (!_method.ReturnsVoid)
                                {
                                    WriteReturn(true);
                                }
                                EmitTree(conversionOperatorDeclarationSyntax.ExpressionBody.Expression,
                                    cancellationToken);
                                WriteSemiColon(true);
                            }
                            else if (conversionOperatorDeclarationSyntax.Body != null)
                            {
                                WriteLocalParameters(conversionOperatorDeclarationSyntax.Body, cancellationToken);
                                foreach (var statement in conversionOperatorDeclarationSyntax.Body.Statements)
                                {
                                    EmitTree(statement, cancellationToken);
                                }
                            }
                        }
                        else if (operatorDeclarationSyntax != null)
                        {
                            WriteNewLine();
                            BeginBlock();

                            if (operatorDeclarationSyntax.ExpressionBody != null)
                            {
                                if (!_method.ReturnsVoid)
                                {
                                    WriteReturn(true);
                                }
                                EmitTree(operatorDeclarationSyntax.ExpressionBody.Expression,
                                    cancellationToken);
                                WriteSemiColon(true);
                            }
                            else if (operatorDeclarationSyntax.Body != null)
                            {
                                WriteLocalParameters(operatorDeclarationSyntax.Body, cancellationToken);
                                foreach (var statement in operatorDeclarationSyntax.Body.Statements)
                                {
                                    EmitTree(statement, cancellationToken);
                                }
                            }
                        }
                        else if (arrowExpressionClauseSyntax != null)
                        {
                            WriteNewLine();
                            BeginBlock();

                            if (!_method.ReturnsVoid)
                            {
                                WriteReturn(true);
                            }
                            EmitTree(arrowExpressionClauseSyntax.Expression,
                                cancellationToken);
                            WriteSemiColon(true);
                        }
                        else if (constructorDeclarationSyntax != null)
                        {
                            if (!Emitter.IsAbstract(_method.ContainingType))
                            {
                                if (constructorDeclarationSyntax.Initializer != null)
                                {
                                    if (constructorDeclarationSyntax.Initializer.ThisOrBaseKeyword.Kind() ==
                                        SyntaxKind.ThisKeyword)
                                    {
                                        Write(" : this");
                                    }
                                    else
                                    {
                                        Write(" : super");
                                    }

                                    var ctor = (IMethodSymbol)Emitter
                                        .GetSymbolInfo(constructorDeclarationSyntax.Initializer)
                                        .Symbol;

                                    WriteMethodInvocation(ctor,
                                        constructorDeclarationSyntax.Initializer.ArgumentList,
                                        constructorDeclarationSyntax.Initializer,
                                        cancellationToken);
                                    WriteNewLine();
                                }
                            }
                            else
                            {
                                WriteNewLine();
                            }
                            BeginBlock();

                            if (constructorDeclarationSyntax.ExpressionBody != null)
                            {
                                EmitTree(constructorDeclarationSyntax.ExpressionBody);
                                WriteSemiColon(true);
                            }
                            if (constructorDeclarationSyntax.Body != null)
                            {
                                WriteLocalParameters(constructorDeclarationSyntax.Body, cancellationToken);
                                foreach (var statement in constructorDeclarationSyntax.Body.Statements)
                                {
                                    EmitTree(statement, cancellationToken);
                                }
                            }
                        }
                        else if (accessorDeclarationSyntax != null)
                        {
                            WriteNewLine();
                            BeginBlock();

                            if (accessorDeclarationSyntax.ExpressionBody != null)
                            {
                                if (!_method.ReturnsVoid || _method.MethodKind == MethodKind.PropertySet)
                                {
                                    WriteReturn(true);
                                }
                                EmitTree(accessorDeclarationSyntax.ExpressionBody.Expression,
                                    cancellationToken);
                                WriteSemiColon(true);
                            }
                            else if (accessorDeclarationSyntax.Body != null)
                            {
                                WriteLocalParameters(accessorDeclarationSyntax.Body, cancellationToken);
                                EmitterContext.SetterMethod =
                                    _method.MethodKind == MethodKind.PropertySet ? _method : null;
                                foreach (var statement in accessorDeclarationSyntax.Body.Statements)
                                {
                                    EmitTree(statement, cancellationToken);
                                }

                                EmitterContext.SetterMethod = null;

                                if (_method.MethodKind == MethodKind.PropertySet)
                                {
                                    WriteReturn(true);
                                    var property = (IPropertySymbol)_method.AssociatedSymbol;
                                    if (property.GetMethod != null)
                                    {
                                        Write(Emitter.GetMethodName(property.GetMethod));
                                        WriteOpenParentheses();
                                        if (property.IsIndexer)
                                        {
                                            for (int i = 0; i < property.GetMethod.Parameters.Length; i++)
                                            {
                                                if (i > 0)
                                                {
                                                    WriteComma();
                                                }
                                                Write(property.GetMethod.Parameters[i].Name);
                                            }
                                        }
                                        WriteCloseParentheses();
                                    }
                                    else
                                    {
                                        Write(_method.Parameters.Last().Name);
                                    }
                                    WriteSemiColon(true);
                                }
                            }
                            else
                            {
                                WriteNewLine();
                                BeginBlock();

                                WriteDefaultImplementation(_method);

                            }
                        }
                        else
                        {
                            Debug.Fail($"Unhandled syntax node: {node.Kind()}");
                        }
                    }
                }
                else
                {
                    WriteNewLine();
                    BeginBlock();
                    WriteDefaultImplementation(_method);
                }

                EmitterContext.IsInMethodBody = false;
                EndBlock();
                WriteNewLine();
            }

            WriteComments(_method, false, cancellationToken);
        }

        private void WriteLocalParameters(BlockSyntax body, CancellationToken cancellationToken)
        {
            EmitterContext.IsInMethodBody = true;

            EmitterContext.BuildLocalParameters(_method, body, cancellationToken);
            foreach (var parameter in EmitterContext.ParameterNames)
            {
                Write("var ", EmitterContext.GetSymbolName(parameter.Key));
                WriteSpace();
                WriteColon();
                WriteType(parameter.Key.Type);

                Write(" = ");

                Write(parameter.Key.Name);
                WriteSemiColon(true);
            }
        }

        private void WriteDefaultImplementation(IMethodSymbol method)
        {
            if (_method.MethodKind == MethodKind.PropertyGet)
            {
                WriteAutoPropertyGetter();
            }
            else if (_method.MethodKind == MethodKind.PropertySet)
            {
                WriteAutoPropertySetter();
            }
            if (_method.MethodKind == MethodKind.EventAdd)
            {
                WriteDefaultEventAdder();
            }
            else if (_method.MethodKind == MethodKind.EventRemove)
            {
                WriteDefaultEventRemover();
            }
        }

        private void WriteDefaultEventRemover()
        {
            var property = (IEventSymbol)_method.AssociatedSymbol;
            Write(Emitter.GetEventName(property));
            Write(" = ");
            WriteEventType((INamedTypeSymbol)property.Type, false);
            Write(".remove(");
            Write(Emitter.GetEventName(property));
            Write(", ");
            Write(_method.Parameters[0].Name);
            Write(")");
            WriteSemiColon();
            WriteNewLine();
        }

        private void WriteDefaultEventAdder()
        {
            var property = (IEventSymbol)_method.AssociatedSymbol;
            Write(Emitter.GetEventName(property));
            Write(" = ");
            WriteEventType((INamedTypeSymbol)property.Type, false);
            Write(".combine(");
            Write(Emitter.GetEventName(property));
            Write(", ");
            Write(_method.Parameters[0].Name);
            Write(")");
            WriteSemiColon();
            WriteNewLine();
        }

        private void CollectTypeParameters(List<ITypeSymbol> typeParameters, ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.TypeParameter)
            {
                if (!typeParameters.Contains(type))
                {
                    typeParameters.Add(type);
                }
            }
            else if (type is INamedTypeSymbol)
            {
                var named = (INamedTypeSymbol)type;
                foreach (var argument in named.TypeArguments)
                {
                    CollectTypeParameters(typeParameters, argument);
                }
            }
        }

        private void WriteAutoPropertySetter()
        {
            var backingField = _method.ContainingType
                                .GetMembers()
                                .OfType<IFieldSymbol>()
                                .FirstOrDefault(f => f.AssociatedSymbol == _method.AssociatedSymbol);
            var property = (IPropertySymbol)_method.AssociatedSymbol;
            if (backingField == null)
            {
                Write("// TODO: autoproperty set");
            }
            else
            {
                Write("return ");
                Write(Emitter.GetFieldName(backingField));
                Write(" = ");
                Write(_method.Parameters[0].Name);
                WriteSemiColon();
            }
            WriteNewLine();
        }

        private void WriteAutoPropertyGetter()
        {
            var backingField = _method.ContainingType
                    .GetMembers()
                    .OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.AssociatedSymbol == _method.AssociatedSymbol);
            var property = (IPropertySymbol)_method.AssociatedSymbol;
            if (backingField == null)
            {
                Write("// TODO: autoproperty get");
            }
            else
            {
                Write("return ");
                Write(Emitter.GetFieldName(backingField));
                WriteSemiColon();
            }
            WriteNewLine();
        }
    }
}

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

namespace Phase.Translator.Java
{
    public class MethodBlock : AbstractJavaEmitterBlock
    {
        private readonly IMethodSymbol _method;

        public MethodBlock(JavaEmitterContext context, IMethodSymbol method)
            : base(context)
        {
            _method = method;
        }

        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Emitter.IsExternal(_method)
                || Emitter.IsCompilerExtension(_method)
                || (_method.MethodKind == MethodKind.PropertyGet && Emitter.IsExternal(_method.AssociatedSymbol))
                || (_method.MethodKind == MethodKind.PropertySet && Emitter.IsExternal(_method.AssociatedSymbol))
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


            if (_method.OverriddenMethod != null && _method.OverriddenMethod.ContainingType.SpecialType != SpecialType.System_Object && !Emitter.IsAbstract(_method.ContainingType))
            {
                Write("@Override");
                WriteNewLine();
            }

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

            if (_method.IsStatic && _method.MethodKind != MethodKind.StaticConstructor)
            {
                Write("static ");
            }

            if (_method.IsAbstract)
            {
                Write("abstract ");
            }

            var typeParameters = new List<ITypeSymbol>(_method.TypeParameters);
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
                    Write(typeParameters[i].Name);
                }
                Write("> ");
            }

            switch (_method.MethodKind)
            {
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                    WriteType(((IPropertySymbol)_method.AssociatedSymbol).Type);
                    break;
                case MethodKind.EventAdd:
                case MethodKind.EventRemove:
                    WriteEventType((INamedTypeSymbol)((IEventSymbol)_method.AssociatedSymbol).Type);
                    break;
                case MethodKind.Constructor:
                case MethodKind.StaticConstructor:
                    break;
                default:
                    if (Emitter.IsGetEnumeratorAsIterator(_method))
                    {
                        Write("Iterable<");
                        var generic = ((INamedTypeSymbol)_method.ReturnType).TypeArguments[0];
                        WriteType(generic);
                        Write(">");
                    }
                    else
                    {
                        WriteType(_method.ReturnType);
                    }
                    break;
            }

            Write(" ");

            if (_method.MethodKind != MethodKind.StaticConstructor)
            {
                var methodName = Emitter.GetMethodName(_method);
                Write(methodName);
                WriteOpenParentheses();
                WriteParameterDeclarations(_method.Parameters, cancellationToken);
                WriteCloseParentheses();
                WriteSpace();
            }

            if (_method.ContainingType.TypeKind == TypeKind.Interface || (Emitter.IsNative(_method.ContainingType) && _method.IsExtern) || _method.IsAbstract)
            {
                WriteSemiColon(true);
            }
            else
            {
                WriteNewLine();
                BeginBlock();

                if (!_method.DeclaringSyntaxReferences.IsEmpty)
                {
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
                                foreach (var statement in methodDeclarationSyntax.Body.Statements)
                                {
                                    EmitTree(statement, cancellationToken);
                                }
                            }
                        }
                        else if (conversionOperatorDeclarationSyntax != null)
                        {
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
                                foreach (var statement in conversionOperatorDeclarationSyntax.Body.Statements)
                                {
                                    EmitTree(statement, cancellationToken);
                                }
                            }
                        }
                        else if (operatorDeclarationSyntax != null)
                        {
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
                                foreach (var statement in operatorDeclarationSyntax.Body.Statements)
                                {
                                    EmitTree(statement, cancellationToken);
                                }
                            }
                        }
                        else if (arrowExpressionClauseSyntax != null)
                        {
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
                                        Write("this");
                                    }
                                    else
                                    {
                                        Write("super");
                                    }

                                    var ctor = (IMethodSymbol)Emitter
                                        .GetSymbolInfo(constructorDeclarationSyntax.Initializer)
                                        .Symbol;

                                    WriteMethodInvocation(ctor,
                                        constructorDeclarationSyntax.Initializer.ArgumentList,
                                        constructorDeclarationSyntax.Initializer,
                                        cancellationToken);
                                    WriteSemiColon(true);
                                }
                            }

                            if (constructorDeclarationSyntax.ExpressionBody != null)
                            {
                                EmitTree(constructorDeclarationSyntax.ExpressionBody);
                                WriteSemiColon(true);
                            }
                            if (constructorDeclarationSyntax.Body != null)
                            {
                                foreach (var statement in constructorDeclarationSyntax.Body.Statements)
                                {
                                    EmitTree(statement, cancellationToken);
                                }
                            }
                        }
                        else if (accessorDeclarationSyntax != null)
                        {
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
                    WriteDefaultImplementation(_method);
                }

                EndBlock();
                WriteNewLine();
            }

            WriteComments(_method, false, cancellationToken);
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
            Write("return ");
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
            Write("return ");
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

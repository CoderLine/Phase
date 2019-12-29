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
using Phase.Translator.Utils;

namespace Phase.Translator.TypeScript
{
    public class MethodBlock : AbstractTypeScriptEmitterBlock
    {
        private readonly IMethodSymbol _method;

        public MethodBlock(TypeScriptEmitterContext context, IMethodSymbol method)
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
                || (_method.MethodKind == MethodKind.PropertyGet && !((IPropertySymbol) _method.AssociatedSymbol)
                        .ExplicitInterfaceImplementations.IsEmpty)
                || (_method.MethodKind == MethodKind.PropertySet && !((IPropertySymbol) _method.AssociatedSymbol)
                        .ExplicitInterfaceImplementations.IsEmpty)
            )
            {
                return;
            }

            switch (_method.MethodKind)
            {
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                    if (Emitter.IsAutoProperty((IPropertySymbol) _method.AssociatedSymbol))
                    {
                        return;
                    }

                    break;
                case MethodKind.EventRaise:
                    if (Emitter.IsEventField((IEventSymbol) _method.AssociatedSymbol))
                    {
                        return;
                    }

                    break;
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

            if (_method.ContainingType.TypeKind == TypeKind.Struct && Emitter.IsAbstract(_method.ContainingType) &&
                _method.MethodKind == MethodKind.Constructor && _method.DeclaringSyntaxReferences.Length == 0)
            {
                // implicit constructor for structs
                return;
            }
            if (_method.ContainingType.TypeKind == TypeKind.Interface)
            {
                switch (_method.MethodKind)
                {
                    case MethodKind.EventAdd:
                    case MethodKind.EventRemove:
                    case MethodKind.EventRaise:
                    case MethodKind.PropertyGet:
                    case MethodKind.PropertySet:
                        return;
                }
            }

            WriteComments(_method, cancellationToken);
            WriteMeta(_method, cancellationToken);

            if (_method.ContainingType.TypeKind != TypeKind.Interface)
            {
                if (_method.MethodKind == MethodKind.StaticConstructor)
                {
                    WriteAccessibility(Accessibility.Public);
                }
                else
                {
                    switch (_method.MethodKind)
                    {
                        case MethodKind.PropertySet:
                            WriteAccessibility(((IPropertySymbol)_method.AssociatedSymbol).GetMethod.DeclaredAccessibility);
                            break;
                        default:
                            WriteAccessibility(_method.DeclaredAccessibility);
                            break;
                    }
                }
            }

            if (_method.AssociatedSymbol is IPropertySymbol prop && !prop.IsIndexer)
            {
                switch (_method.MethodKind)
                {
                    case MethodKind.PropertyGet:
                        Write("get ");
                        break;
                    case MethodKind.PropertySet:
                        Write("set ");
                        break;
                }
            }

            if (_method.IsStatic)
            {
                Write("static ");
            }

            var methodName = EmitterContext.GetMethodName(_method);
            Write(methodName);

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

                Write(">");
            }

            WriteOpenParentheses();
            WriteParameterDeclarations(_method.Parameters, cancellationToken);
            WriteCloseParentheses();
            WriteSpace();

            switch (_method.MethodKind)
            {
                case MethodKind.PropertyGet:
                    WriteColon();
                    Write(Emitter.GetTypeNameWithNullability(((IPropertySymbol) _method.AssociatedSymbol).Type));
                    EmitterContext.ImportType(((IPropertySymbol) _method.AssociatedSymbol).Type);
                    break;
                case MethodKind.PropertySet:
                    break;
                case MethodKind.EventAdd:
                case MethodKind.EventRemove:
                    WriteColon();
                    WriteType(Emitter.GetSpecialType(SpecialType.System_Void));
                    break;
                case MethodKind.Constructor:
                case MethodKind.StaticConstructor:
                    break;
                default:
                    WriteColon();
                    if (Emitter.IsGetEnumeratorAsIterator(_method))
                    {
                        Write("Iterable<");
                        var generic = ((INamedTypeSymbol) _method.ReturnType).TypeArguments[0];
                        WriteType(generic);
                        EmitterContext.ImportType(generic);
                        Write(">");
                    }
                    else
                    {
                        Write(Emitter.GetTypeNameWithNullability(_method.ReturnType));
                        EmitterContext.ImportType(_method.ReturnType);
                    }

                    Write(" ");
                    break;
            }

            if (_method.ContainingType.TypeKind == TypeKind.Interface ||
                (Emitter.IsNative(_method.ContainingType) && _method.IsExtern))
            {
                WriteSemiColon(true);
            }
            else
            {
                BeginBlock();

                if (_method.DeclaringSyntaxReferences.IsEmpty && _method.MethodKind == MethodKind.Constructor &&
                    !_method.IsStatic && _method.ContainingType.BaseType != null &&
                    _method.ContainingType.BaseType.SpecialType != SpecialType.System_Object)
                {
                    // default constructor 
                    var x = EmitterContext.GetMethodName(_method);
                    if (x == "constructor")
                    {
                        if (!Emitter.HasNoConstructor(_method.ContainingType.BaseType))
                        {
                            Write("super");
                            WriteOpenCloseParentheses();
                            WriteSemiColon(true);
                        }
                    }
                    else
                    {
                        Write("this.", x);
                        WriteOpenCloseParentheses();
                        WriteSemiColon(true);
                    }
                }
                else if (!_method.DeclaringSyntaxReferences.IsEmpty)
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
                            else if (_method.IsAbstract)
                            {
                                Write("throw \"abstract\";");
                                WriteNewLine();
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
                            else if (_method.IsAbstract)
                            {
                                Write("throw \"abstract\";");
                                WriteNewLine();
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
                            else if (_method.IsAbstract)
                            {
                                Write("throw \"abstract\";");
                                WriteNewLine();
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
                                    var ctor = (IMethodSymbol) Emitter
                                        .GetSymbolInfo(constructorDeclarationSyntax.Initializer)
                                        .Symbol;

                                    var x = EmitterContext.GetMethodName(ctor);
                                    if (x == "constructor")
                                    {
                                        Write("super");
                                    }
                                    else
                                    {
                                        Write("this.", x);
                                    }

                                    WriteMethodInvocation(ctor,
                                        constructorDeclarationSyntax.Initializer.ArgumentList,
                                        constructorDeclarationSyntax.Initializer,
                                        cancellationToken);
                                    WriteSemiColon(true);
                                }
                                else if (!_method.IsStatic && _method.ContainingType.BaseType != null &&
                                         _method.ContainingType.BaseType.SpecialType != SpecialType.System_Object)
                                {
                                    var ctor = _method.ContainingType.BaseType.InstanceConstructors.FirstOrDefault(
                                        c => c.Parameters.Length == 0);
                                    if (ctor != null)
                                    {
                                        var x = EmitterContext.GetMethodName(ctor);
                                        if (x == "constructor")
                                        {
                                            if (!Emitter.HasNoConstructor(_method.ContainingType.BaseType))
                                            {
                                                Write("super");
                                                WriteOpenCloseParentheses();
                                                WriteSemiColon(true);
                                            }
                                        }
                                        else
                                        {
                                            Write(x);
                                            WriteOpenCloseParentheses();
                                            WriteSemiColon(true);
                                        }
                                    }
                                    else
                                    {
                                        Debugger.Break();
                                    }
                                }
                            }

                            // write default initializers
                            WriteDefaultInitializers(_method.ContainingType, _method.IsStatic, cancellationToken);
                            if (_method.IsStatic)
                            {
                                WriteES5PropertyDeclarations(_method.ContainingType);
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
                                if (_method.MethodKind == MethodKind.PropertyGet)
                                {
                                    WriteReturn(true);
                                    EmitTree(accessorDeclarationSyntax.ExpressionBody.Expression,
                                        cancellationToken);
                                    WriteSemiColon(true);
                                }
                                else if (_method.MethodKind == MethodKind.PropertySet)
                                {
                                    var property = (IPropertySymbol) _method.AssociatedSymbol;
                                    if (property.GetMethod != null)
                                    {
                                        var typeInfo =
                                            Emitter.GetTypeInfo(accessorDeclarationSyntax.ExpressionBody.Expression);
                                        if (SymbolEquivalenceComparer.Instance.Equals(typeInfo.Type, property.Type))
                                        {
                                            EmitTree(accessorDeclarationSyntax.ExpressionBody.Expression,
                                                cancellationToken);
                                            WriteSemiColon(true);
                                        }
                                        else
                                        {
                                            EmitTree(accessorDeclarationSyntax.ExpressionBody.Expression,
                                                cancellationToken);
                                            WriteSemiColon(true);
                                        }
                                    }
                                }
                                else
                                {
                                    EmitTree(accessorDeclarationSyntax.ExpressionBody.Expression,
                                        cancellationToken);
                                    WriteSemiColon(true);
                                }
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
                            }
                            else
                            {
                                WriteDefaultImplementation(cancellationToken);
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
                    WriteDefaultImplementation(cancellationToken);
                }

                if (_method.MethodKind == MethodKind.Constructor &&
                    !Emitter.HasNativeConstructors(_method.ContainingType) &&
                    Emitter.HasConstructorOverloads(_method.ContainingType))
                {
                    WriteReturn(true);
                    WriteThis();
                    WriteSemiColon(true);
                }

                EndBlock();
                WriteNewLine();
            }

            WriteComments(_method, false, cancellationToken);
        }

        private void WriteDefaultImplementation(CancellationToken cancellationToken)
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
            else if (_method.MethodKind == MethodKind.Constructor)
            {
                WriteDefaultInitializers(_method.ContainingType, _method.IsStatic, cancellationToken);
            }
        }

        private void WriteDefaultEventRemover()
        {
            var property = (IEventSymbol) _method.AssociatedSymbol;
            if (property.IsAbstract)
            {
                Write("throw \"abstract\";");
                WriteNewLine();
            }
            else
            {
                Write("this.", Emitter.GetEventName(property));
                Write(" = ");
                WriteEventType((INamedTypeSymbol) property.Type, false);
                Write(".remove(this.", Emitter.GetEventName(property), ", ", _method.Parameters[0].Name, ")");
                WriteSemiColon();
            }

            WriteNewLine();
        }

        private void WriteDefaultEventAdder()
        {
            var property = (IEventSymbol) _method.AssociatedSymbol;
            if (property.IsAbstract)
            {
                Write("throw \"abstract\";");
                WriteNewLine();
            }
            else
            {
                Write("this.", Emitter.GetEventName(property));
                Write(" = ");
                WriteEventType((INamedTypeSymbol) property.Type, false);
                Write(".combine(this.", Emitter.GetEventName(property), ", ", _method.Parameters[0].Name, ")");
                WriteSemiColon();
            }

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
                var named = (INamedTypeSymbol) type;
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
            var property = (IPropertySymbol) _method.AssociatedSymbol;
            if (property.IsAbstract)
            {
                Write("throw \"abstract\";");
                WriteNewLine();
            }
            else if (backingField == null)
            {
                Write("// TODO: autoproperty set");
            }
            else
            {
                Write("this.");
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
            var property = (IPropertySymbol) _method.AssociatedSymbol;
            if (property.IsAbstract)
            {
                Write("throw \"abstract\";");
                WriteNewLine();
            }
            else if (backingField == null)
            {
                Write("// TODO: autoproperty get");
            }
            else
            {
                Write("return this.");
                Write(Emitter.GetFieldName(backingField));
                WriteSemiColon();
            }

            WriteNewLine();
        }
    }
}
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

namespace Phase.Translator.Haxe
{
    public class MethodBlock : AbstractHaxeScriptEmitterBlock
    {
        private readonly IMethodSymbol _method;

        public MethodBlock(HaxeEmitterContext context, IMethodSymbol method)
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

            switch (_method.MethodKind)
            {
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                    if (Emitter.IsAutoProperty((IPropertySymbol)_method.AssociatedSymbol))
                    {
                        return;
                    }
                    break;
                case MethodKind.EventAdd:
                case MethodKind.EventRemove:
                case MethodKind.EventRaise:
                    if (Emitter.IsEventField((IEventSymbol)_method.AssociatedSymbol))
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

            if (_method.ContainingType.TypeKind == TypeKind.Struct && Emitter.IsAbstract(_method.ContainingType) && _method.MethodKind == MethodKind.Constructor && _method.DeclaringSyntaxReferences.Length == 0)
            {
                // implicit constructor for structs
                return;
            }

            WriteComments(_method, cancellationToken);

            if (Emitter.IsFrom(_method))
            {
                Write("@:from ");
            }

            if (Emitter.IsTo(_method))
            {
                Write("@:to ");
            }

            var op = Emitter.GetOp(_method);
            if (op != null)
            {
                Write("@:op");
                WriteOpenParentheses();
                Write(op);
                WriteCloseParentheses();
            }

            if (_method.MethodKind == MethodKind.StaticConstructor || _method.ContainingType.TypeKind == TypeKind.Interface)
            {
                WriteAccessibility(Accessibility.Public);
            }
            else
            {
                WriteAccessibility(_method.DeclaredAccessibility);
            }

            if (_method.IsStatic)
            {
                Write("static ");
            }

            if (Emitter.IsInline(_method))
            {
                Write("inline ");
            }

            if (_method.OverriddenMethod != null && _method.OverriddenMethod.ContainingType.SpecialType != SpecialType.System_Object && !Emitter.IsAbstract(_method.ContainingType))
            {
                Write("override ");
            }


            Write("function ");

            var methodName = Emitter.GetMethodName(_method);
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
                case MethodKind.PropertySet:
                    WriteColon();
                    WriteType(((IPropertySymbol)_method.AssociatedSymbol).Type);
                    break;
                case MethodKind.EventAdd:
                case MethodKind.EventRemove:
                    WriteColon();
                    WriteEventType((INamedTypeSymbol)((IEventSymbol)_method.AssociatedSymbol).Type);
                    break;
                case MethodKind.Constructor:
                case MethodKind.StaticConstructor:
                    break;
                default:
                    WriteColon();
                    WriteType(_method.ReturnType);
                    Write(" ");
                    break;
            }

            if (_method.ContainingType.TypeKind == TypeKind.Interface)
            {
                WriteSemiColon(true);
            }
            else
            {
                WriteNewLine();
                BeginBlock();

                if (_method.DeclaringSyntaxReferences.IsEmpty && _method.MethodKind == MethodKind.Constructor && !_method.IsStatic && _method.ContainingType.BaseType != null && _method.ContainingType.BaseType.SpecialType != SpecialType.System_Object)
                {
                    // default constructor 
                    var x = Emitter.GetMethodName(_method);
                    if (x == "new")
                    {
                        Write("super");
                    }
                    else
                    {
                        Write(x);
                    }

                    WriteOpenCloseParentheses();
                    WriteSemiColon(true);
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
                                EmitTree(methodDeclarationSyntax.ExpressionBody.Expression,
                                    cancellationToken);
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
                                EmitTree(conversionOperatorDeclarationSyntax.ExpressionBody.Expression,
                                    cancellationToken);
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
                                EmitTree(operatorDeclarationSyntax.ExpressionBody.Expression,
                                    cancellationToken);
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
                            if (_method.ReturnsVoid)
                            {
                                EmitTree(arrowExpressionClauseSyntax.Expression,
                                    cancellationToken);
                            }
                            else
                            {
                                WriteReturn(true);
                                EmitTree(arrowExpressionClauseSyntax.Expression,
                                    cancellationToken);
                                WriteSemiColon(true);
                            }
                        }
                        else if (constructorDeclarationSyntax != null)
                        {
                            if (!Emitter.IsAbstract(_method.ContainingType))
                            {
                                if (constructorDeclarationSyntax.Initializer != null)
                                {
                                    var ctor = (IMethodSymbol)Emitter
                                        .GetSymbolInfo(constructorDeclarationSyntax.Initializer)
                                        .Symbol;

                                    var x = Emitter.GetMethodName(ctor);
                                    if (x == "new")
                                    {
                                        Write("super");
                                    }
                                    else
                                    {
                                        Write(x);
                                    }

                                    WriteMethodInvocation(ctor,
                                        constructorDeclarationSyntax.Initializer.ArgumentList,
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
                                        var x = Emitter.GetMethodName(ctor);
                                        if (x == "new")
                                        {
                                            Write("super");
                                        }
                                        else
                                        {
                                            Write(x);
                                        }
                                        WriteOpenCloseParentheses();
                                        WriteSemiColon(true);
                                    }
                                    else
                                    {
                                        Debugger.Break();
                                    }
                                }
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
                            if (accessorDeclarationSyntax.Body != null)
                            {
                                foreach (var statement in accessorDeclarationSyntax.Body.Statements)
                                {
                                    EmitTree(statement, cancellationToken);
                                }

                                if (_method.MethodKind == MethodKind.PropertySet)
                                {
                                    Write("return ");
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

                if (_method.MethodKind == MethodKind.Constructor && !Emitter.HasNativeConstructors(_method.ContainingType) && Emitter.HasConstructorOverloads(_method.ContainingType))
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
            if (property.IsAbstract)
            {
                Write("throw \"abstract\";");
                WriteNewLine();
            }
            else
            {
                Write("return (");
                Write(Emitter.GetEventName(property));
                Write(" -= ");
                Write(_method.Parameters[0].Name);
                Write(")");
                WriteSemiColon();
            }
            WriteNewLine();

        }

        private void WriteDefaultEventAdder()
        {
            var property = (IEventSymbol)_method.AssociatedSymbol;
            if (property.IsAbstract)
            {
                Write("throw \"abstract\";");
                WriteNewLine();
            }
            else
            {
                Write("return (");
                Write(Emitter.GetEventName(property));
                Write(" += ");
                Write(_method.Parameters[0].Name);
                Write(")");
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
            var property = (IPropertySymbol)_method.AssociatedSymbol;
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
                Write("return ");
                Write(Emitter.GetFieldName(backingField));
                WriteSemiColon();
            }
            WriteNewLine();
        }
    }
}

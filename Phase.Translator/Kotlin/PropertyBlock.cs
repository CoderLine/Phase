using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.Kotlin
{
    public class PropertyBlock : AbstractKotlinEmitterBlock
    {
        private readonly IPropertySymbol _property;

        public PropertyBlock(KotlinEmitterContext context, IPropertySymbol property)
            : base(context)
        {
            _property = property;
        }

        protected override void DoEmit(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_property.IsIndexer) return;
            PropertyDeclarationSyntax declaration = null;
            foreach (var d in _property.DeclaringSyntaxReferences)
            {
                var syntax = (PropertyDeclarationSyntax)d.GetSyntax(cancellationToken);
                if (!syntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    declaration = syntax;
                    break;
                }
            }

            WriteComments(_property, cancellationToken);
            WriteMeta(_property, cancellationToken);
            if (_property.IsStatic)
            {
                Write("@JvmStatic");
                WriteNewLine();
            }

            if (_property.IsOverride || Emitter.IsInterfaceImplementation(_property))
            {
                Write("override ");
            }
            else if (_property.IsVirtual)
            {
                Write("open ");
            }
            if (_property.IsAbstract && _property.ContainingType.TypeKind != TypeKind.Interface)
            {
                Write("abstract ");
            }
            

            WriteAccessibility(_property.DeclaredAccessibility);
            if (_property.SetMethod != null)
            {
                Write("var ");
            }
            else
            {
                Write("val ");
            }

            var autoProperty = Emitter.IsAutoProperty(_property);
            var propertyName = Emitter.GetPropertyName(_property);
            Write(" ", propertyName, " : ");
            WriteType(_property.Type);

            if (declaration != null && _property.ContainingType.TypeKind != TypeKind.Interface && !_property.IsAbstract)
            {
                if (declaration.Initializer != null)
                {
                    Write(" = ");
                    EmitTree(declaration.Initializer.Value);
                }
                else if (autoProperty && _property.SetMethod != null)
                {
                    Write(" = ");
                    Write(Emitter.GetDefaultValue(_property.Type));
                }
                WriteNewLine();

                if (declaration.ExpressionBody != null)
                {
                    Write("get() = ");
                    EmitTree(declaration.ExpressionBody);
                }
                else
                {
                    AccessorDeclarationSyntax getter = null;
                    AccessorDeclarationSyntax setter = null;
                    foreach (var accessor in declaration.AccessorList.Accessors)
                    {
                        if (accessor.Keyword.Kind() == SyntaxKind.GetKeyword)
                        {
                            getter = accessor;
                        }
                        else if (accessor.Keyword.Kind() == SyntaxKind.SetKeyword)
                        {
                            setter = accessor;
                        }
                    }


                    if (getter != null)
                    {
                        if (getter.Modifiers.Any(SyntaxKind.PrivateKeyword) || getter.Modifiers.Any(SyntaxKind.ProtectedKeyword))
                        {
                            Write("protected ");
                            if (autoProperty)
                            {
                                Write("get");
                                WriteNewLine();
                            }
                        }

                        if (!autoProperty)
                        {
                            Write("get()");

                            if (getter.ExpressionBody != null)
                            {
                                Write(" = ");
                                EmitTree(getter.ExpressionBody);
                            }
                            else if (getter.Body != null)
                            {
                                EmitTree(getter.Body);
                            }
                            else if(!_property.IsAbstract)
                            {
                                Write(" = ");
                                var backingField = _property.ContainingType
                                    .GetMembers()
                                    .OfType<IFieldSymbol>()
                                    .FirstOrDefault(f => f.AssociatedSymbol == _property);
                                if (backingField == null)
                                {
                                    Write("/* TODO */");
                                }
                                else
                                {
                                    Write(Emitter.GetFieldName(backingField));
                                }
                            }
                            WriteNewLine();
                        }
                    }

                    if (setter != null)
                    {
                        if (setter.Modifiers.Any(SyntaxKind.PrivateKeyword) || setter.Modifiers.Any(SyntaxKind.ProtectedKeyword))
                        {
                            Write("protected ");
                            if (autoProperty)
                            {
                                Write("set");
                                WriteNewLine();
                            }
                        }

                        if (!autoProperty)
                        {
                            Write("set(v)");
                            BeginBlock();
                            if (setter.ExpressionBody != null)
                            {
                                Write("var value = v;");
                                WriteNewLine();
                                EmitTree(setter.ExpressionBody);
                            }
                            else if (setter.Body != null)
                            {
                                Write("var value = v;");
                                WriteNewLine();
                                foreach (var statement in setter.Body.Statements)
                                {
                                    EmitTree(statement);
                                }
                            }
                            else if (!_property.IsAbstract)
                            {
                                var backingField = _property.ContainingType
                                    .GetMembers()
                                    .OfType<IFieldSymbol>()
                                    .FirstOrDefault(f => f.AssociatedSymbol == _property);
                                if (backingField == null)
                                {
                                    Write("/* TODO */");
                                }
                                else
                                {
                                    Write(Emitter.GetFieldName(backingField), " = v");
                                    WriteSemiColon();
                                }
                                WriteNewLine();
                            }
                            EndBlock(true);
                        }
                    }
                }
            }
            WriteNewLine();
        }
    }
}
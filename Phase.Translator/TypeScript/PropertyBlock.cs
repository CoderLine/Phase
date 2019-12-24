using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phase.Translator.TypeScript
{
    public class PropertyBlock : AbstractTypeScriptEmitterBlock
    {
        private readonly IPropertySymbol _property;

        public PropertyBlock(TypeScriptEmitterContext context, IPropertySymbol property)
            : base(context)
        {
            _property = property;
        }

        protected override void DoEmit(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Emitter.IsExternal(_property))
            {
                return;
            }

            if (!_property.ExplicitInterfaceImplementations.IsEmpty)
            {
                return;
            }


            if (!_property.IsIndexer && _property.OverriddenProperty == null)
            {
                var isAutoProperty = Emitter.IsAutoProperty(_property);
                if (isAutoProperty)
                {
                    WriteComments(_property, cancellationToken);
                    WriteMeta(_property, cancellationToken);
                    WriteAccessibility(_property.DeclaredAccessibility);

                    if (_property.IsStatic)
                    {
                        Write("static ");
                    }
                    
                    if (_property.SetMethod == null)
                    {
                        Write("readonly ");
                    }

                    var propertyName = Emitter.GetPropertyName(_property);
                    Write(propertyName);
                    WriteColon();
                    Write(Emitter.GetTypeNameWithNullability(_property.Type));
                    EmitterContext.ImportType(_property.Type);

                    var initializer = _property.DeclaringSyntaxReferences
                        .Select(r => ((PropertyDeclarationSyntax) r.GetSyntax(cancellationToken)).Initializer)
                        .FirstOrDefault(p => p != null);

                    if (initializer != null)
                    {
                        Write(" = ");
                        EmitTree(initializer);
                    }

                    WriteSemiColon(true);
                    WriteNewLine();

                    WriteComments(_property, false, cancellationToken);
                }
            }
        }
    }
}
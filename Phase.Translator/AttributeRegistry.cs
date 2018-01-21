using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Phase.Translator
{
    public class AttributeRegistry
    {
        private readonly ConcurrentDictionary<ISymbol, List<AttributeData>> _registeredAttributes;
        private readonly ConcurrentDictionary<ISymbol, List<AttributeData>> _registeredReturnAttributes;

        public AttributeRegistry()
        {
            _registeredAttributes = new ConcurrentDictionary<ISymbol, List<AttributeData>>();
            _registeredReturnAttributes = new ConcurrentDictionary<ISymbol, List<AttributeData>>();
        }

        public void RegisterAttribute(ISymbol symbol, AttributeData attribute)
        {
            if (!_registeredAttributes.TryGetValue(symbol, out var attributes))
            {
                _registeredAttributes[symbol] = attributes = new List<AttributeData>();
            }
            attributes.Add(attribute);
        }

        public void RegisterReturnValueAttribute(ISymbol symbol, AttributeData attribute)
        {
            if (!_registeredReturnAttributes.TryGetValue(symbol, out var attributes))
            {
                _registeredReturnAttributes[symbol] = attributes = new List<AttributeData>();
            }
            attributes.Add(attribute);
        }

        public IEnumerable<AttributeData> GetAttributes(ISymbol symbol)
        {
            var attributes = Enumerable.Empty<AttributeData>();

            if (_registeredAttributes.TryGetValue(symbol, out var registeredAttributes))
            {
                attributes = attributes.Concat(registeredAttributes);
            }

            return attributes.Concat(symbol.GetAttributes());
        }
        public IEnumerable<AttributeData> GetReturnAttributes(ISymbol symbol)
        {
            var attributes = Enumerable.Empty<AttributeData>();

            if (_registeredReturnAttributes.TryGetValue(symbol, out var registeredAttributes))
            {
                attributes = attributes.Concat(registeredAttributes);
            }

            return attributes.Concat(symbol.GetAttributes());
        }
    }
}

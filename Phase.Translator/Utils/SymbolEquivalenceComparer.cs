using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Utils
{
    public class SymbolEquivalenceComparer : IEqualityComparer<ISymbol>
    {
        public static readonly SymbolEquivalenceComparer Instance = new SymbolEquivalenceComparer();

        public bool Equals(ISymbol x, ISymbol y)
        {
            return GetId(x).Equals(GetId(y));
        }

        public int GetHashCode(ISymbol obj)
        {
            return GetId(obj).GetHashCode();
        }

        public void Reset()
        {
            _idCache.Clear();
        }


        private ConcurrentDictionary<ISymbol, string> _idCache = new ConcurrentDictionary<ISymbol, string>();
        private string GetId(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    symbol = ((IMethodSymbol)symbol).ReducedFrom ?? symbol;
                    break;
                case SymbolKind.Alias:
                case SymbolKind.ArrayType:
                case SymbolKind.Assembly:
                case SymbolKind.DynamicType:
                case SymbolKind.ErrorType:
                case SymbolKind.Event:
                case SymbolKind.Field:
                case SymbolKind.Label:
                case SymbolKind.Local:
                case SymbolKind.NetModule:
                case SymbolKind.NamedType:
                case SymbolKind.Namespace:
                case SymbolKind.Parameter:
                case SymbolKind.PointerType:
                case SymbolKind.Property:
                case SymbolKind.RangeVariable:
                case SymbolKind.TypeParameter:
                case SymbolKind.Preprocessing:
                case SymbolKind.Discard:
                default:
                    break;
            }

            symbol = symbol.OriginalDefinition;
            if (_idCache.TryGetValue(symbol, out var id))
            {
                return id;
            }

            var sb = new StringBuilder();

            void appendParent()
            {
                if (symbol.ContainingSymbol != null)
                {
                    var parentId = GetId(symbol.ContainingSymbol);
                    if (!string.IsNullOrEmpty(parentId))
                    {
                        sb.Append(parentId);
                        sb.Append(".");
                    }
                }
            }

            switch (symbol.Kind)
            {
                case SymbolKind.NetModule:
                    break;
                case SymbolKind.Alias:
                case SymbolKind.DynamicType:
                case SymbolKind.ErrorType:
                case SymbolKind.Event:
                case SymbolKind.Field:
                case SymbolKind.Label:
                case SymbolKind.Local:
                case SymbolKind.Namespace:
                case SymbolKind.Parameter:
                case SymbolKind.PointerType:
                case SymbolKind.Property:
                case SymbolKind.RangeVariable:
                case SymbolKind.TypeParameter:
                case SymbolKind.Preprocessing:
                case SymbolKind.Discard:
                    appendParent();
                    sb.Append(symbol.Name);
                    break;
                case SymbolKind.Assembly:
                    sb.Append(symbol.Name);
                    break;
                case SymbolKind.ArrayType:
                    appendParent();
                    var arrayType = (IArrayTypeSymbol)symbol;
                    sb.Append(GetId(arrayType.ElementType));
                    sb.Append($"[`{arrayType.Rank}]");
                    break;
                case SymbolKind.NamedType:
                    appendParent();
                    var namedType = (INamedTypeSymbol)symbol;
                    sb.Append(namedType.Name);
                    if (namedType.TypeParameters.Length > 0)
                    {
                        sb.Append("`" + namedType.TypeParameters.Length);
                    }
                    break;
                case SymbolKind.Method:
                    appendParent();
                    var method = (IMethodSymbol)symbol;
                    sb.Append(method.Name);
                    if (method.TypeParameters.Length > 0)
                    {
                        sb.Append("`" + method.TypeParameters.Length);
                    }
                    sb.Append("(");
                    for (int i = 0; i < method.Parameters.Length; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        var t = method.Parameters[i].Type;
                        if (t.TypeKind != TypeKind.TypeParameter)
                        {
                            sb.Append(GetId(t));
                        }
                        else
                        {
                            sb.Append(t.Name);
                        }
                    }
                    sb.Append(")");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return _idCache[symbol] = sb.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Utils
{
    static class Utils
    {
        public static string ToCamelCase(this string s)
        {
            if (s == s.ToUpperInvariant())
            {
                return s.ToLower();
            }
            return char.ToLower(s[0]) + s.Substring(1);
        }

        public static string ToPascalCase(this string s)
        {
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public static T FindParent<T>(this SyntaxNode node)
            where T :SyntaxNode
        {
            while (node.Parent != null)
            {
                if (node.Parent is T tp)
                {
                    return tp;
                }

                node = node.Parent;
            }

            return null;
        }
}
}

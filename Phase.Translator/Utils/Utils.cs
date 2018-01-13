using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}

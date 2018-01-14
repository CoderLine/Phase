using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Phase.Translator.Utils
{
    public class CodeTemplate
    {
        private static Regex FormatArgRegex = new Regex(@"\{(?<Name>\w+)(:(?<Modifier>\w+))?\}");

        private readonly string _template;
        public Dictionary<string, CodeTemplateVariable> Variables { get; }
        public bool SkipSemicolonOnStatements { get; }

        public CodeTemplate(string template, bool skipSemicolonOnStatements)
        {
            SkipSemicolonOnStatements = skipSemicolonOnStatements;
            _template = template;
            Variables = new Dictionary<string, CodeTemplateVariable>();

            var matches = FormatArgRegex.Matches(template);
            foreach (Match match in matches)
            {
                RegisterVariable(BuildVariable(match));
            }
        }

        private CodeTemplateVariable BuildVariable(Match match)
        {
            var variable = new CodeTemplateVariable();
            variable.Name = match.Groups["Name"].Value;
            if (match.Groups["Modifier"].Success)
            {
                variable.Modifier = match.Groups["Modifier"].Value;
            }
            return variable;
        }

        private void RegisterVariable(CodeTemplateVariable variable)
        {
            Variables[variable.Name] = variable;
        }

        public override string ToString()
        {
            return FormatArgRegex.Replace(_template, match =>
            {
                var variable = BuildVariable(match);
                if (!Variables.TryGetValue(variable.Name, out variable))
                {
                    Debug.Fail("All variables should have been emitted and filled by now");
                    variable.RawValue = "null/*missing value*/";
                }
                return variable.RawValue;
            });
        }
    }


    public class CodeTemplateVariable
    {
        public string Name { get; set; }
        public string Modifier { get; set; }
        public string RawValue { get; set; }

        public CodeTemplateVariable()
        {
            Name = string.Empty;
            RawValue = string.Empty;
        }
    }
}

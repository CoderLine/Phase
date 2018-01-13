using System.Collections.Generic;
using System.Xml;
using Microsoft.CodeAnalysis;

namespace Phase.Translator
{
    public class ExternalAttributes
    {
        private readonly Dictionary<string, List<ExternalAttribute>> _attributes;

        public ExternalAttributes()
        {
            _attributes = new Dictionary<string, List<ExternalAttribute>>();
        }

        public void Import(string fileName)
        {
            var xml = new XmlDocument();
            xml.Load(fileName);

            var root = xml.DocumentElement;
            if (root != null && root.Name == "assembly")
            {
                ImportAssembly(root);
            }
        }

        private void ImportAssembly(XmlElement assembly)
        {
            foreach (XmlElement member in assembly.GetElementsByTagName("member"))
            {
                ImportMember(member);
            }
        }

        private void ImportMember(XmlElement member)
        {
            var name = member.GetAttribute("name");
            if (!string.IsNullOrEmpty(name))
            {
                var attributes = ReadAttributes(member);
                if (attributes.Count > 0)
                {
                    _attributes[name] = attributes;
                }

                foreach (XmlElement parameter in member.GetElementsByTagName("parameter"))
                {
                    ImportParameter(name, parameter);
                }
            }
        }

        private void ImportParameter(string methodName, XmlElement parameter)
        {
            var name = parameter.GetAttribute("name");
            if (!string.IsNullOrEmpty(name))
            {
                var attributes = ReadAttributes(parameter);
                if (attributes.Count > 0)
                {
                    _attributes[methodName + "." + name] = attributes;
                }
            }
        }

        private List<ExternalAttribute> ReadAttributes(XmlElement member)
        {
            var attributes = new List<ExternalAttribute>();
            foreach (XmlElement attributeNode in member.GetElementsByTagName("attribute"))
            {
                var attribute = ImportAttribute(attributeNode);
                if (attribute != null)
                {
                    attributes.Add(attribute);
                }
            }
            return attributes;
        }

        private ExternalAttribute ImportAttribute(XmlElement attributeNode)
        {
            var attribute = new ExternalAttribute();

            foreach (XmlAttribute xmlAttribute in attributeNode.Attributes)
            {
                if (xmlAttribute.Name == "ctor")
                {
                    attribute.Type = xmlAttribute.Value;
                }
                else
                {
                    attribute.Parameters[xmlAttribute.Name] = xmlAttribute.Value;
                }
            }

            if (!string.IsNullOrEmpty(attribute.Type))
            {
                return attribute;
            }
            return null;
        }

        public class ExternalAttribute
        {
            public string Type { get; set; }
            public Dictionary<string, string> Parameters { get; set; }

            public ExternalAttribute()
            {
                Parameters = new Dictionary<string, string>();
            }
        }

        #region GetAttributes

        public List<ExternalAttribute> GetAttributes(string xmlDocId)
        {
            List<ExternalAttribute> attributes;
            if (!_attributes.TryGetValue(xmlDocId, out attributes))
            {
                attributes = null;
            }
            return attributes;
        }

        public List<ExternalAttribute> GetAttributes(ISymbol type)
        {
            return GetAttributes(this.GetXmlDocId(type));
        }

        #endregion

        #region HasAttributes

        public bool HasAttributes(string xmlDocId)
        {
            return _attributes.ContainsKey(xmlDocId);
        }

        public bool HasAttributes(ISymbol declaration)
        {
            return HasAttributes(GetXmlDocId(declaration));
        }
        
        #endregion

        #region GetXmlDocId

        public string GetXmlDocId(ISymbol type)
        {
            return type.GetDocumentationCommentId();
        }

        #endregion
    }
}

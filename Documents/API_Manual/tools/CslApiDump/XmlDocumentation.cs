using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace CslApiDump
{
    /// <summary>
    /// Reads the XML documentation file that the compiler produces next to the
    /// assembly and makes the descriptions available by documentation id.
    /// </summary>
    internal sealed class XmlDocumentation
    {
        private readonly Dictionary<string, XElement> _members =
            new Dictionary<string, XElement>(StringComparer.Ordinal);

        public XmlDocumentation(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The XML documentation file was not found.", path);
            }

            var document = XDocument.Load(path);
            foreach (var member in document.Descendants("member"))
            {
                var name = (string)member.Attribute("name");
                if (!string.IsNullOrEmpty(name))
                {
                    _members[name] = member;
                }
            }
        }

        public int Count => _members.Count;

        /// <summary>
        /// Gets the documentation of a member. The exact documentation id is
        /// tried first; when it is not found (for example for an overload whose
        /// parameter list differs) the first entry of the same member name is
        /// used instead.
        /// </summary>
        public XElement Find(string documentationId, string fallbackPrefix)
        {
            XElement element;
            if (_members.TryGetValue(documentationId, out element))
            {
                return element;
            }

            if (!string.IsNullOrEmpty(fallbackPrefix))
            {
                foreach (var pair in _members)
                {
                    if (pair.Key.StartsWith(fallbackPrefix, StringComparison.Ordinal))
                    {
                        return pair.Value;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Builds the documentation ids used by the compiler for the XML
    /// documentation file.
    /// </summary>
    internal static class DocumentationId
    {
        public static string ForType(Type type)
        {
            return "T:" + TypeName(type);
        }

        public static string ForMethod(Type type, string methodName, ParameterInfo[] parameters)
        {
            // The compiler omits the parameter list of a method without
            // parameters, so "M:Namespace.Type.Method" has no brackets.
            if (parameters.Length == 0)
            {
                return "M:" + TypeName(type) + "." + methodName;
            }

            return "M:" + TypeName(type) + "." + methodName + "(" +
                   string.Join(",", parameters.Select(p => TypeName(p.ParameterType))) + ")";
        }

        public static string ForConstructor(Type type, ParameterInfo[] parameters)
        {
            if (parameters.Length == 0)
            {
                return "M:" + TypeName(type) + ".#ctor";
            }

            return "M:" + TypeName(type) + ".#ctor(" +
                   string.Join(",", parameters.Select(p => TypeName(p.ParameterType))) + ")";
        }

        public static string ForProperty(Type type, string name)
        {
            return "P:" + TypeName(type) + "." + name;
        }

        public static string ForField(Type type, string name)
        {
            return "F:" + TypeName(type) + "." + name;
        }

        public static string ForEvent(Type type, string name)
        {
            return "E:" + TypeName(type) + "." + name;
        }

        /// <summary>
        /// Formats a type the way the compiler does in a documentation id, for
        /// example <c>System.Collections.Generic.List`1{System.String}</c>.
        /// </summary>
        public static string TypeName(Type type)
        {
            if (type.IsByRef)
            {
                return TypeName(type.GetElementType()) + "@";
            }

            if (type.IsPointer)
            {
                return TypeName(type.GetElementType()) + "*";
            }

            if (type.IsArray)
            {
                return TypeName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            }

            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            var builder = new StringBuilder();

            var namespaceName = type.Namespace;
            if (!string.IsNullOrEmpty(namespaceName))
            {
                builder.Append(namespaceName).Append('.');
            }

            var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            var chain = new List<Type>();
            for (var current = definition; current != null; current = current.DeclaringType)
            {
                chain.Insert(0, current);
            }

            for (var index = 0; index < chain.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append('.');
                }

                var link = chain[index];
                builder.Append(link.Name);

                if (link.IsGenericTypeDefinition)
                {
                    var arity = link.GetGenericArguments().Length;
                    if (arity > 0)
                    {
                        builder.Append('`').Append(arity);
                    }
                }
            }

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                builder.Append('{')
                       .Append(string.Join(",", type.GetGenericArguments().Select(TypeName)))
                       .Append('}');
            }

            return builder.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace CslApiDump
{
    internal enum ApiMemberKind
    {
        Method,
        Constructor,
        Property,
        Field,
        Event,
        EnumValue,
    }

    internal sealed class ApiParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
    }

    internal sealed class ApiMember
    {
        public ApiMemberKind Kind { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string Returns { get; set; } = string.Empty;
        public bool IsStatic { get; set; }
        public List<ApiParameter> Parameters { get; set; } = new List<ApiParameter>();
        public List<string> Exceptions { get; set; } = new List<string>();
    }

    internal sealed class ApiType
    {
        public string FullName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string BaseType { get; set; } = string.Empty;
        public List<string> Interfaces { get; set; } = new List<string>();
        public string Summary { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public List<ApiMember> Members { get; set; } = new List<ApiMember>();

        /// <summary>Set when the type is reached through a property of another type.</summary>
        public string ObtainedFrom { get; set; } = string.Empty;
    }

    /// <summary>
    /// Builds the documented API model from the compiled assembly and the XML
    /// documentation file.
    /// </summary>
    internal sealed class ApiModelBuilder
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance |
                                          BindingFlags.Static | BindingFlags.DeclaredOnly;

        private readonly XmlDocumentation _documentation;

        public ApiModelBuilder(XmlDocumentation documentation)
        {
            _documentation = documentation;
        }

        public List<ApiType> Build(Assembly assembly, Func<string, string> obtainHints)
        {
            var types = new List<ApiType>();

            foreach (var type in assembly.GetExportedTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                types.Add(BuildType(type, obtainHints));
            }

            return types;
        }

        private ApiType BuildType(Type type, Func<string, string> obtainHints)
        {
            var model = new ApiType
            {
                FullName = type.FullName,
                Namespace = type.Namespace ?? string.Empty,
                Name = type.Name,
                Kind = DescribeKind(type),
                BaseType = type.BaseType != null ? FriendlyName(type.BaseType) : string.Empty,
                Interfaces = type.GetInterfaces().Select(FriendlyName).OrderBy(n => n, StringComparer.Ordinal).ToList(),
                ObtainedFrom = obtainHints(type.FullName),
            };

            var typeElement = _documentation.Find(DocumentationId.ForType(type), "T:" + type.FullName.Replace('+', '.') + ".");
            model.Summary = Text(typeElement, "summary");
            model.Remarks = Text(typeElement, "remarks");

            foreach (var member in BuildMembers(type))
            {
                model.Members.Add(member);
            }

            return model;
        }

        private static string DescribeKind(Type type)
        {
            if (type.IsEnum) return "enum";
            if (type.IsInterface) return "interface";
            if (typeof(Delegate).IsAssignableFrom(type)) return "delegate";
            if (type.IsValueType) return "struct";
            return "class";
        }

        private IEnumerable<ApiMember> BuildMembers(Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance |
                                       BindingFlags.Static | BindingFlags.DeclaredOnly;

            if (type.IsEnum)
            {
                foreach (var field in type.GetFields(flags).Where(f => f.IsLiteral))
                {
                    var element = _documentation.Find(
                        DocumentationId.ForField(type, field.Name),
                        "F:" + type.FullName.Replace('+', '.') + "." + field.Name);

                    yield return new ApiMember
                    {
                        Kind = ApiMemberKind.EnumValue,
                        Name = field.Name,
                        Signature = field.Name + " = " + Convert.ToString(field.GetRawConstantValue()),
                        Summary = Text(element, "summary"),
                        Remarks = Text(element, "remarks"),
                    };
                }

                yield break;
            }

            foreach (var constructor in type.GetConstructors(flags))
            {
                var element = _documentation.Find(
                    DocumentationId.ForConstructor(type, constructor.GetParameters()),
                    "M:" + type.FullName.Replace('+', '.') + ".#ctor(");

                var member = new ApiMember
                {
                    Kind = ApiMemberKind.Constructor,
                    Name = type.Name,
                    Signature = FriendlyName(type) + "(" + ParameterList(constructor.GetParameters()) + ")",
                    Summary = Text(element, "summary"),
                    Remarks = Text(element, "remarks"),
                    Returns = Text(element, "returns"),
                };

                FillParameters(member, element, constructor.GetParameters());
                yield return member;
            }

            foreach (var property in type.GetProperties(flags))
            {
                var element = _documentation.Find(
                    DocumentationId.ForProperty(type, property.Name),
                    "P:" + type.FullName.Replace('+', '.') + "." + property.Name);

                var accessor = property.CanRead ? "get; " : string.Empty;
                if (property.CanWrite) accessor += "set; ";

                yield return new ApiMember
                {
                    Kind = ApiMemberKind.Property,
                    Name = property.Name,
                    Signature = FriendlyName(property.PropertyType) + " " + property.Name + " { " + accessor + "}",
                    Summary = Text(element, "summary"),
                    Remarks = Text(element, "remarks"),
                    IsStatic = (property.GetMethod ?? property.SetMethod).IsStatic,
                };
            }

            foreach (var field in type.GetFields(flags))
            {
                var element = _documentation.Find(
                    DocumentationId.ForField(type, field.Name),
                    "F:" + type.FullName.Replace('+', '.') + "." + field.Name);

                yield return new ApiMember
                {
                    Kind = ApiMemberKind.Field,
                    Name = field.Name,
                    Signature = FriendlyName(field.FieldType) + " " + field.Name +
                               (field.IsLiteral ? " = " + Convert.ToString(field.GetRawConstantValue()) : string.Empty) + ";",
                    Summary = Text(element, "summary"),
                    Remarks = Text(element, "remarks"),
                    IsStatic = field.IsStatic,
                };
            }

            foreach (var declaredEvent in type.GetEvents(flags))
            {
                var element = _documentation.Find(
                    DocumentationId.ForEvent(type, declaredEvent.Name),
                    "E:" + type.FullName.Replace('+', '.') + "." + declaredEvent.Name);

                yield return new ApiMember
                {
                    Kind = ApiMemberKind.Event,
                    Name = declaredEvent.Name,
                    Signature = "event " + FriendlyName(declaredEvent.EventHandlerType) + " " + declaredEvent.Name,
                    Summary = Text(element, "summary"),
                    Remarks = Text(element, "remarks"),
                    IsStatic = declaredEvent.AddMethod.IsStatic,
                };
            }

            foreach (var method in type.GetMethods(flags).Where(m => !m.IsSpecialName))
            {
                var parameters = method.GetParameters();
                var element = _documentation.Find(
                    DocumentationId.ForMethod(type, method.Name, parameters),
                    "M:" + type.FullName.Replace('+', '.') + "." + method.Name + "(");

                var member = new ApiMember
                {
                    Kind = ApiMemberKind.Method,
                    Name = method.Name,
                    Signature = FriendlyName(method.ReturnType) + " " + method.Name + "(" +
                               ParameterList(parameters) + ")",
                    Summary = Text(element, "summary"),
                    Remarks = Text(element, "remarks"),
                    Returns = Text(element, "returns"),
                    IsStatic = method.IsStatic,
                };

                FillParameters(member, element, parameters);

                if (element != null)
                {
                    foreach (var exception in element.Elements("exception"))
                    {
                        var typeName = (string)exception.Attribute("cref");
                        if (!string.IsNullOrEmpty(typeName))
                        {
                            member.Exceptions.Add(StripCref(typeName));
                        }
                    }
                }

                yield return member;
            }
        }

        private static void FillParameters(ApiMember member, XElement element, ParameterInfo[] parameters)
        {
            foreach (var parameter in parameters)
            {
                var summary = string.Empty;
                if (element != null)
                {
                    var tag = element.Elements("param")
                        .FirstOrDefault(e => (string)e.Attribute("name") == parameter.Name);
                    if (tag != null)
                    {
                        summary = RenderText(tag);
                    }
                }

                member.Parameters.Add(new ApiParameter
                {
                    Name = parameter.Name,
                    Type = FriendlyName(parameter.ParameterType),
                    Summary = summary,
                    DefaultValue = parameter.HasDefaultValue ? FormatDefault(parameter.DefaultValue) : string.Empty,
                });
            }
        }

        private static string FormatDefault(object value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is string text)
            {
                return "\"" + text + "\"";
            }

            if (value is bool flag)
            {
                return flag ? "true" : "false";
            }

            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string ParameterList(ParameterInfo[] parameters)
        {
            return string.Join(", ", parameters.Select(p =>
            {
                var modifier = p.IsOut ? "out " : p.ParameterType.IsByRef ? "ref " : string.Empty;
                var text = modifier + FriendlyName(p.ParameterType) + " " + p.Name;
                if (p.HasDefaultValue)
                {
                    text += " = " + FormatDefault(p.DefaultValue);
                }

                return text;
            }));
        }

        /// <summary>
        /// The C# keywords of the primitive types, so that the manual reads
        /// like C# source and not like a reflection dump.
        /// </summary>
        private static readonly Dictionary<string, string> Keywords = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "System.Void", "void" },
            { "System.Boolean", "bool" },
            { "System.Byte", "byte" },
            { "System.SByte", "sbyte" },
            { "System.Char", "char" },
            { "System.Decimal", "decimal" },
            { "System.Double", "double" },
            { "System.Single", "float" },
            { "System.Int32", "int" },
            { "System.UInt32", "uint" },
            { "System.Int64", "long" },
            { "System.UInt64", "ulong" },
            { "System.Int16", "short" },
            { "System.UInt16", "ushort" },
            { "System.Object", "object" },
            { "System.String", "string" },
        };

        /// <summary>
        /// Formats a type the way it is written in C# source.
        /// </summary>
        public static string FriendlyName(Type type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            if (type.IsByRef)
            {
                return FriendlyName(type.GetElementType());
            }

            if (type.IsArray)
            {
                return FriendlyName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            }

            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            var keyword = Keywords.TryGetValue(type.FullName ?? string.Empty, out var name) ? name : null;
            if (keyword != null)
            {
                return keyword;
            }

            var builder = new StringBuilder();
            var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            builder.Append(definition.Name);

            var tick = builder.ToString().IndexOf('`');
            if (tick >= 0)
            {
                builder.Length = tick;
            }

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                var arguments = type.GetGenericArguments();
                var own = arguments.Take(definition.GetGenericArguments().Length).ToArray();
                builder.Append('<').Append(string.Join(", ", own.Select(FriendlyName))).Append('>');
            }

            return builder.ToString();
        }

        private static string StripCref(string cref)
        {
            var text = cref;
            var parenthesis = text.IndexOf('(');
            if (parenthesis > 0)
            {
                text = text.Substring(0, parenthesis);
            }

            var lastDot = text.LastIndexOf('.');
            return lastDot >= 0 ? text.Substring(lastDot + 1) : text;
        }

        private static string Text(XElement element, string name)
        {
            if (element == null)
            {
                return string.Empty;
            }

            var child = element.Elements(name).FirstOrDefault();
            if (child == null)
            {
                return string.Empty;
            }

            return RenderText(child);
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var lines = value.Replace("\r\n", "\n").Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToList();

            return string.Join(" ", lines).Trim();
        }

        /// <summary>
        /// Renders an XML documentation element as plain text. The inline
        /// elements of a documentation comment are replaced by their content so
        /// that a reference such as <c>&lt;see cref="RFIDReader.Options"/&gt;</c>
        /// reads as "RFIDReader.Options" instead of disappearing.
        /// </summary>
        private static string RenderText(XElement element)
        {
            if (element == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            foreach (var node in element.Nodes())
            {
                var text = node as XText;
                if (text != null)
                {
                    builder.Append(text.Value);
                    continue;
                }

                var child = node as XElement;
                if (child == null)
                {
                    continue;
                }

                switch (child.Name.LocalName)
                {
                    case "see":
                    case "seealso":
                        {
                            var cref = (string)child.Attribute("cref");
                            var langword = (string)child.Attribute("langword");
                            if (!string.IsNullOrEmpty(langword))
                            {
                                builder.Append(langword);
                            }
                            else if (!string.IsNullOrEmpty(cref))
                            {
                                builder.Append(SimplifyCref(cref));
                            }
                            else
                            {
                                builder.Append(RenderText(child));
                            }

                            break;
                        }

                    case "paramref":
                    case "typeparamref":
                        {
                            var name = (string)child.Attribute("name");
                            builder.Append(name ?? string.Empty);
                            break;
                        }

                    case "para":
                        builder.Append(RenderText(child)).Append(' ');
                        break;

                    default:
                        builder.Append(RenderText(child));
                        break;
                }
            }

            return Normalize(builder.ToString());
        }

        /// <summary>
        /// Shortens a documentation reference to the name of the member it
        /// points at, for example "M:CSLibrary.RFIDReader.Options" becomes
        /// "RFIDReader.Options".
        /// </summary>
        private static string SimplifyCref(string cref)
        {
            var text = cref;
            var parenthesis = text.IndexOf('(');
            if (parenthesis > 0)
            {
                text = text.Substring(0, parenthesis);
            }

            if (text.Length > 2 && text[1] == ':')
            {
                text = text.Substring(2);
            }

            var parts = text.Split('.');
            if (parts.Length <= 1)
            {
                return text;
            }

            return parts[parts.Length - 2] + "." + parts[parts.Length - 1];
        }
    }
}

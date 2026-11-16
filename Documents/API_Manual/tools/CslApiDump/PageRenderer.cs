using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CslApiDump
{
    /// <summary>
    /// Writes the API reference pages.
    /// </summary>
    internal sealed class PageRenderer
    {
        private readonly ApiLayout _layout;
        private readonly Func<string, string> _obtainHints;

        public PageRenderer(ApiLayout layout, Func<string, string> obtainHints)
        {
            _layout = layout;
            _obtainHints = obtainHints;
        }

        public void Write(string rootDirectory, IReadOnlyList<ApiType> types)
        {
            var index = types.ToDictionary(t => t.FullName, StringComparer.Ordinal);

            foreach (var page in _layout.Pages)
            {
                if (page.Handwritten)
                {
                    // The page is written by hand and only appears in the navigation.
                    continue;
                }

                var body = new StringBuilder();
                body.Append("<h1>").Append(Html.Escape(page.Title)).Append("</h1>");
                if (!string.IsNullOrEmpty(page.Summary))
                {
                    body.Append("<p class=\"lede\">").Append(Html.Escape(page.Summary)).Append("</p>");
                }

                var documented = new List<ApiType>();
                foreach (var typeName in page.Types)
                {
                    ApiType type;
                    if (!index.TryGetValue(typeName, out type))
                    {
                        throw new InvalidOperationException(
                            "The layout refers to the type '" + typeName + "' which is not part of the public API.");
                    }

                    documented.Add(type);
                }

                foreach (var type in documented)
                {
                    AppendType(body, type, page);
                }

                WritePage(rootDirectory, page.File, page.Title, body.ToString());
            }

            WriteTypeIndex(rootDirectory, types);
            WriteNavigation(rootDirectory);
        }

        private void AppendType(StringBuilder body, ApiType type, ApiPage page)
        {
            body.Append("<section class=\"type\" id=\"").Append(Html.Escape(type.FullName)).Append("\">");
            body.Append("<h2>").Append(Html.Escape(type.Name)).Append("</h2>");

            body.Append("<table class=\"meta\"><tbody>");
            body.Append("<tr><th>Namespace</th><td>").Append(Html.Code(type.Namespace)).Append("</td></tr>");
            body.Append("<tr><th>Kind</th><td>").Append(Html.Escape(type.Kind)).Append("</td></tr>");
            if (!string.IsNullOrEmpty(type.BaseType) && type.BaseType != "Object")
            {
                body.Append("<tr><th>Base type</th><td>").Append(Html.Code(type.BaseType)).Append("</td></tr>");
            }

            if (type.Interfaces.Count > 0)
            {
                body.Append("<tr><th>Implements</th><td>")
                    .Append(string.Join(", ", type.Interfaces.Select(Html.Code)))
                    .Append("</td></tr>");
            }

            if (!string.IsNullOrEmpty(type.ObtainedFrom))
            {
                body.Append("<tr><th>Obtain</th><td>").Append(Html.Code(type.ObtainedFrom)).Append("</td></tr>");
            }

            body.Append("</tbody></table>");

            body.Append(Html.Paragraph(type.Summary));
            body.Append(Html.Paragraph(type.Remarks));

            var groups = page.MemberGroups;
            if (groups.Count > 0)
            {
                foreach (var group in groups)
                {
                    var members = type.Members
                        .Where(m => group.Members.Contains(m.Name, StringComparer.Ordinal))
                        .ToList();

                    if (members.Count == 0)
                    {
                        continue;
                    }

                    body.Append("<h3 id=\"").Append(Html.Escape(Slug(group.Title)))
                        .Append("\">").Append(Html.Escape(group.Title)).Append("</h3>");
                    if (!string.IsNullOrEmpty(group.Summary))
                    {
                        body.Append("<p class=\"lede\">").Append(Html.Escape(group.Summary)).Append("</p>");
                    }

                    AppendMembers(body, type, members);
                }
            }
            else if (type.Members.Count > 0)
            {
                AppendMembers(body, type, type.Members);
            }

            body.Append("</section>");
        }

        private static void AppendMembers(StringBuilder body, ApiType type, IEnumerable<ApiMember> members)
        {
            body.Append("<table class=\"members\"><thead><tr><th class=\"col-member\">Member</th>")
                .Append("<th class=\"col-description\">Description</th></tr></thead><tbody>");

            foreach (var member in members)
            {
                body.Append("<tr><td><code class=\"signature\">")
                    .Append(Html.Escape(member.Signature))
                    .Append("</code>");

                if (member.IsStatic)
                {
                    body.Append("<span class=\"badge\">static</span>");
                }

                body.Append("</td><td>");

                if (!string.IsNullOrEmpty(member.Summary))
                {
                    body.Append("<p>").Append(Html.Escape(member.Summary)).Append("</p>");
                }

                if (member.Parameters.Count > 0)
                {
                    body.Append("<table class=\"params\"><thead><tr><th>Parameter</th><th>Type</th>")
                        .Append("<th>Description</th></tr></thead><tbody>");
                    foreach (var parameter in member.Parameters)
                    {
                        body.Append("<tr><td><code>").Append(Html.Escape(parameter.Name)).Append("</code>");
                        if (!string.IsNullOrEmpty(parameter.DefaultValue))
                        {
                            body.Append("<span class=\"default\">= ").Append(Html.Escape(parameter.DefaultValue)).Append("</span>");
                        }

                        body.Append("</td><td><code>").Append(Html.Escape(parameter.Type)).Append("</code></td><td>")
                            .Append(Html.Escape(parameter.Summary)).Append("</td></tr>");
                    }

                    body.Append("</tbody></table>");
                }

                if (!string.IsNullOrEmpty(member.Returns))
                {
                    body.Append("<p class=\"returns\"><span class=\"label\">Returns</span> ")
                        .Append(Html.Escape(member.Returns)).Append("</p>");
                }

                if (member.Exceptions.Count > 0)
                {
                    body.Append("<p class=\"returns\"><span class=\"label\">Throws</span> ")
                        .Append(Html.Escape(string.Join(", ", member.Exceptions))).Append("</p>");
                }

                if (!string.IsNullOrEmpty(member.Remarks))
                {
                    body.Append("<p class=\"remarks\">").Append(Html.Escape(member.Remarks)).Append("</p>");
                }

                body.Append("</td></tr>");
            }

            body.Append("</tbody></table>");
        }

        private void WriteTypeIndex(string rootDirectory, IReadOnlyList<ApiType> types)
        {
            var page = _layout.Pages.FirstOrDefault(p => p.Id == "api-index");
            var file = page?.File ?? "api/index.html";
            var title = page?.Title ?? "API index";

            var body = new StringBuilder();
            body.Append("<h1>").Append(Html.Escape(title)).Append("</h1>");
            body.Append("<p class=\"lede\">Every type of the application API, with the page that documents it.</p>");

            var pageOfType = new Dictionary<string, ApiPage>(StringComparer.Ordinal);
            foreach (var layoutPage in _layout.Pages)
            {
                foreach (var typeName in layoutPage.Types)
                {
                    pageOfType[typeName] = layoutPage;
                }
            }

            body.Append("<table class=\"index\"><thead><tr><th>Type</th><th>Kind</th><th>Namespace</th>")
                .Append("<th>Description</th></tr></thead><tbody>");

            foreach (var type in types.OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                ApiPage owner;
                var link = pageOfType.TryGetValue(type.FullName, out owner)
                    ? "<a href=\"" + Html.Escape("../" + owner.File) + "\">" + Html.Escape(owner.Title) + "</a>"
                    : string.Empty;

                body.Append("<tr><td><code>").Append(Html.Escape(type.FullName)).Append("</code></td>")
                    .Append("<td>").Append(Html.Escape(type.Kind)).Append("</td>")
                    .Append("<td><code>").Append(Html.Escape(type.Namespace)).Append("</code></td>")
                    .Append("<td>").Append(Html.Escape(type.Summary)).Append(" ")
                    .Append(link).Append("</td></tr>");
            }

            body.Append("</tbody></table>");

            WritePage(rootDirectory, file, title, body.ToString());
        }

        private void WriteNavigation(string rootDirectory)
        {
            var builder = new StringBuilder();
            builder.Append("window.DOCS_NAV = ");
            builder.Append("{");
            builder.Append("\"title\":").Append(JsonString(_layout.Title)).Append(",");
            builder.Append("\"subtitle\":").Append(JsonString(_layout.Subtitle)).Append(",");
            builder.Append("\"sections\":[");

            var sections = new List<string>();
            foreach (var group in _layout.Pages.GroupBy(p => SectionOf(p.File)))
            {
                var items = group.Select(p =>
                {
                    return "{\"href\":" + JsonString(p.File) + ",\"title\":" + JsonString(p.Title) + "}";
                });
                sections.Add("{\"title\":" + JsonString(group.Key) + ",\"items\":[" + string.Join(",", items) + "]}");
            }

            builder.Append(string.Join(",", sections));
            builder.Append("]};");

            WriteText(rootDirectory, Path.Combine("assets", "nav.js"), builder.ToString());
        }

        /// <summary>
        /// Builds the anchor of a heading from its text.
        /// </summary>
        private static string Slug(string text)
        {
            var builder = new System.Text.StringBuilder();
            var previousDash = true;

            foreach (var character in text.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                    previousDash = false;
                }
                else if (!previousDash)
                {
                    builder.Append('-');
                    previousDash = true;
                }
            }

            return builder.ToString().Trim('-');
        }

        private static string SectionOf(string file)
        {
            if (file.StartsWith("api/", StringComparison.Ordinal)) return "API reference";
            if (file.StartsWith("guides/", StringComparison.Ordinal)) return "Guides";
            if (file.StartsWith("appendix/", StringComparison.Ordinal)) return "Appendix";
            return "Overview";
        }

        public void WritePage(string rootDirectory, string relativePath, string title, string body)
        {
            var depth = relativePath.Count(c => c == '/');
            var prefix = depth == 0 ? string.Empty : string.Concat(Enumerable.Repeat("../", depth));

            var html = new StringBuilder();
            html.Append("<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n");
            html.Append("<meta charset=\"utf-8\" />\n");
            html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />\n");
            html.Append("<title>").Append(Html.Escape(title)).Append(" &ndash; ").Append(Html.Escape(_layout.Title)).Append("</title>\n");
            html.Append("<link rel=\"stylesheet\" href=\"").Append(prefix).Append("assets/docs.css\" />\n");
            html.Append("</head>\n<body>\n");
            html.Append("<header class=\"topbar\"><a class=\"brand\" href=\"").Append(prefix).Append("index.html\">")
                .Append(Html.Escape(_layout.Title)).Append("</a>");
            html.Append("<form class=\"searchbox\" action=\"").Append(prefix).Append("search.html\" method=\"get\">")
                .Append("<input type=\"search\" name=\"q\" placeholder=\"Search the API reference\" aria-label=\"Search\" />")
                .Append("</form></header>\n");
            html.Append("<div class=\"layout\">\n");
            html.Append("<nav class=\"sidebar\" id=\"doc-nav\" aria-label=\"Navigation\"></nav>\n");
            html.Append("<main class=\"content\">\n");
            html.Append(body).Append("\n");
            html.Append("</main>\n</div>\n");
            // The page declares how deep it is, so that the navigation links
            // resolve correctly no matter how the manual is opened: over a web
            // server or directly from the file system.
            html.Append("<script>window.DOCS_PREFIX = \"").Append(prefix).Append("\";</script>\n");
            html.Append("<script src=\"").Append(prefix).Append("assets/nav.js\"></script>\n");
            html.Append("<script src=\"").Append(prefix).Append("assets/docs.js\"></script>\n");
            html.Append("</body>\n</html>\n");

            WriteText(rootDirectory, relativePath, html.ToString());
        }

        public static void WriteText(string rootDirectory, string relativePath, string content)
        {
            var path = Path.Combine(rootDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private static string JsonString(string value)
        {
            if (value == null)
            {
                return "\"\"";
            }

            var builder = new StringBuilder("\"");
            foreach (var character in value)
            {
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < ' ')
                        {
                            builder.Append("\\u").Append(((int)character).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(character);
                        }

                        break;
                }
            }

            builder.Append('"');
            return builder.ToString();
        }
    }
}

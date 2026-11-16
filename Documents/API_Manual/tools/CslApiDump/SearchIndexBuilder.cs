using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CslApiDump
{
    /// <summary>
    /// Builds the search index of the manual.
    /// </summary>
    /// <remarks>
    /// The index is written as a plain script that assigns a global variable, so
    /// that the search works when the manual is opened directly from the file
    /// system: a script tag is not subject to the same origin rules as a fetch.
    /// </remarks>
    internal static class SearchIndexBuilder
    {
        private sealed class Section
        {
            public string Anchor { get; set; } = string.Empty;
            public string Heading { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
        }

        public static void Build(string rootDirectory)
        {
            var files = Directory
                .GetFiles(rootDirectory, "*.html", SearchOption.AllDirectories)
                .Where(file => !Path.GetFileName(file).Equals("search.html", StringComparison.OrdinalIgnoreCase))
                .OrderBy(file => file, StringComparer.Ordinal)
                .ToList();

            var pages = new List<string[]>();
            var postings = new Dictionary<string, Dictionary<int, int>>(StringComparer.Ordinal);

            foreach (var file in files)
            {
                var html = File.ReadAllText(file);
                var relative = MakeRelative(rootDirectory, file);

                var title = ExtractTitle(html);
                var sections = ExtractSections(html);

                if (sections.Count == 0)
                {
                    sections.Add(new Section { Anchor = string.Empty, Heading = title, Text = StripTags(html) });
                }

                foreach (var section in sections)
                {
                    var index = pages.Count;
                    pages.Add(new[] { relative + section.Anchor, title, section.Heading, section.Text });

                    // The title and the heading are repeated so that a match in
                    // them counts more than a match in the body text.
                    var tokens = new List<string>();
                    for (var repeat = 0; repeat < 5; repeat++)
                    {
                        tokens.AddRange(Tokenize(title));
                    }

                    for (var repeat = 0; repeat < 3; repeat++)
                    {
                        tokens.AddRange(Tokenize(section.Heading));
                    }

                    tokens.AddRange(Tokenize(section.Text));

                    foreach (var token in tokens)
                    {
                        Dictionary<int, int> counts;
                        if (!postings.TryGetValue(token, out counts))
                        {
                            counts = new Dictionary<int, int>();
                            postings[token] = counts;
                        }

                        int count;
                        counts.TryGetValue(index, out count);
                        counts[index] = count + 1;
                    }
                }
            }

            var builder = new StringBuilder();
            builder.Append("window.DOCS_INDEX = {\n");
            builder.Append("\"pages\": [\n");
            builder.Append(string.Join(",\n", pages.Select(page =>
                "  [" + string.Join(", ", page.Select(JsonString)) + "]")));
            builder.Append("\n],\n");
            builder.Append("\"postings\": {\n");
            builder.Append(string.Join(",\n", postings
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => "  " + JsonString(pair.Key) + ": [" +
                                string.Join(", ", pair.Value.OrderBy(v => v.Key)
                                    .Select(v => "[" + v.Key + ", " + v.Value + "]")) + "]")));
            builder.Append("\n}\n};\n");

            var path = Path.Combine(rootDirectory, "searchindex.js");
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));

            Console.WriteLine("Search index       : " + path);
            Console.WriteLine("Indexed sections   : " + pages.Count);
            Console.WriteLine("Indexed terms      : " + postings.Count);
        }

        private static string MakeRelative(string root, string path)
        {
            var relative = path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relative.Replace(Path.DirectorySeparatorChar, '/');
        }

        private static string ExtractTitle(string html)
        {
            var match = System.Text.RegularExpressions.Regex.Match(html, "<title>(.*?)</title>",
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return string.Empty;
            }

            var title = StripTags(match.Groups[1].Value);
            var separator = title.IndexOf("–");
            if (separator > 0)
            {
                title = title.Substring(0, separator).Trim();
            }

            return title;
        }

        /// <summary>
        /// Splits the page into its sections. A section starts at a heading and
        /// ends at the next heading.
        /// </summary>
        private static List<Section> ExtractSections(string html)
        {
            var sections = new List<Section>();
            var headingPattern = new System.Text.RegularExpressions.Regex(
                "<h[1-3][^>]*>.*?</h[1-3]>", System.Text.RegularExpressions.RegexOptions.Singleline |
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            var matches = headingPattern.Matches(html);
            if (matches.Count == 0)
            {
                return sections;
            }

            for (var index = 0; index < matches.Count; index++)
            {
                var match = matches[index];
                var bodyStart = match.Index + match.Length;
                var bodyEnd = index + 1 < matches.Count ? matches[index + 1].Index : html.Length;

                var idMatch = System.Text.RegularExpressions.Regex.Match(
                    match.Value, "id=\"([^\"]+)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                sections.Add(new Section
                {
                    Anchor = idMatch.Success ? "#" + idMatch.Groups[1].Value : string.Empty,
                    Heading = StripTags(match.Value),
                    Text = StripTags(html.Substring(bodyStart, bodyEnd - bodyStart)),
                });
            }

            return sections;
        }

        /// <summary>
        /// Removes the markup of a piece of HTML and decodes the entities.
        /// </summary>
        private static string StripTags(string html)
        {
            var text = System.Text.RegularExpressions.Regex.Replace(html, "<script.*?</script>", " ",
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, "<style.*?</style>", " ",
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", " ");

            return System.Net.WebUtility.HtmlDecode(text)
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Aggregate(new StringBuilder(), (builder, word) =>
                {
                    if (builder.Length > 0)
                    {
                        builder.Append(' ');
                    }

                    builder.Append(word);
                    return builder;
                })
                .ToString();
        }

        /// <summary>
        /// Splits a text into the terms of the index. A camel case identifier is
        /// indexed both as one word and as its parts, so that a search for
        /// "SetPowerLevel" and a search for "power" both find the member.
        /// </summary>
        public static List<string> Tokenize(string text)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                return tokens;
            }

            foreach (var word in text.Split(new[] { ' ', '\u00a0' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var cleaned = new StringBuilder(word.Length);
                foreach (var character in word)
                {
                    if (char.IsLetterOrDigit(character))
                    {
                        cleaned.Append(char.ToLowerInvariant(character));
                    }
                    else
                    {
                        cleaned.Append(' ');
                    }
                }

                foreach (var part in cleaned.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (part.Length > 1)
                    {
                        tokens.Add(part);
                    }
                }

                foreach (var part in SplitCamelCase(word))
                {
                    if (part.Length > 1)
                    {
                        tokens.Add(part);
                    }
                }
            }

            return tokens;
        }

        private static IEnumerable<string> SplitCamelCase(string word)
        {
            var parts = new List<string>();
            var current = new StringBuilder();

            foreach (var character in word)
            {
                if (char.IsUpper(character) && current.Length > 0)
                {
                    parts.Add(current.ToString().ToLowerInvariant());
                    current.Length = 0;
                }

                if (char.IsLetterOrDigit(character))
                {
                    current.Append(char.ToLowerInvariant(character));
                }
            }

            if (current.Length > 0)
            {
                parts.Add(current.ToString().ToLowerInvariant());
            }

            return parts;
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

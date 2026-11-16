using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CslApiDump
{
    /// <summary>
    /// Checks every link of the manual.
    /// </summary>
    /// <remarks>
    /// The links that the browser follows are not only the ones in the HTML:
    /// the navigation and the search results are built in the browser from
    /// assets/nav.js and searchindex.js. The checker resolves them the same way
    /// the browser does, relative to the directory of the page that holds them.
    /// </remarks>
    internal static class LinkChecker
    {
        public static int Check(string rootDirectory)
        {
            var pages = Directory
                .GetFiles(rootDirectory, "*.html", SearchOption.AllDirectories)
                .Where(file => !file.Contains(Path.DirectorySeparatorChar + "tools" + Path.DirectorySeparatorChar))
                .OrderBy(file => file, StringComparer.Ordinal)
                .ToList();

            var navigation = ReadNavigation(rootDirectory);
            var searchTargets = ReadSearchTargets(rootDirectory);

            var broken = new List<string>();
            var checkedCount = 0;

            foreach (var page in pages)
            {
                var relativePage = MakeRelative(rootDirectory, page);
                var html = File.ReadAllText(page);

                // The links that are written in the page.
                foreach (Match match in Regex.Matches(html, "(?:href|src)=\"([^\"]+)\""))
                {
                    var target = match.Groups[1].Value;
                    if (IsExternal(target))
                    {
                        continue;
                    }

                    checkedCount++;
                    var resolved = Resolve(rootDirectory, page, target);
                    if (resolved != null && !File.Exists(resolved))
                    {
                        broken.Add(relativePage + " -> " + target);
                    }
                }

                // The prefix that the page declares for the navigation.
                var prefix = ReadPrefix(html);
                if (prefix == null)
                {
                    broken.Add(relativePage + " -> <no DOCS_PREFIX declared>");
                    continue;
                }

                // The navigation links, which the browser builds from nav.js.
                foreach (var href in navigation)
                {
                    checkedCount++;
                    var resolved = Resolve(rootDirectory, page, prefix + href);
                    if (resolved != null && !File.Exists(resolved))
                    {
                        broken.Add(relativePage + " -> " + prefix + href);
                    }
                }

                // The search results, which the browser builds from searchindex.js.
                foreach (var href in searchTargets)
                {
                    checkedCount++;
                    var resolved = Resolve(rootDirectory, page, prefix + href);
                    if (resolved != null && !File.Exists(resolved))
                    {
                        broken.Add(relativePage + " -> " + prefix + href);
                    }
                }
            }

            Console.WriteLine("Pages             : " + pages.Count);
            Console.WriteLine("Links checked     : " + checkedCount);
            Console.WriteLine("Broken links      : " + broken.Count);

            foreach (var entry in broken)
            {
                Console.WriteLine("  " + entry);
            }

            return broken.Count == 0 ? 0 : 1;
        }

        private static bool IsExternal(string target)
        {
            return target.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                || target.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                || target.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
                || target.StartsWith("#", StringComparison.Ordinal);
        }

        /// <summary>
        /// Resolves a link of a page the way a browser resolves it.
        /// </summary>
        private static string Resolve(string rootDirectory, string page, string target)
        {
            var path = target.Split('#')[0].Split('?')[0];
            if (path.Length == 0)
            {
                return null;
            }

            if (path.StartsWith("/", StringComparison.Ordinal))
            {
                // An absolute path is relative to the root of the site.
                return Path.GetFullPath(Path.Combine(rootDirectory, path.TrimStart('/')));
            }

            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(page) ?? rootDirectory, path));
        }

        private static string ReadPrefix(string html)
        {
            var match = Regex.Match(html, "window\\.DOCS_PREFIX\\s*=\\s*\"([^\"]*)\"");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static List<string> ReadNavigation(string rootDirectory)
        {
            var path = Path.Combine(rootDirectory, "assets", "nav.js");
            if (!File.Exists(path))
            {
                return new List<string>();
            }

            var js = File.ReadAllText(path);
            return Regex.Matches(js, "\\{\"href\":\"([^\"]+)\"")
                .Cast<Match>()
                .Select(match => match.Groups[1].Value)
                .ToList();
        }

        private static List<string> ReadSearchTargets(string rootDirectory)
        {
            var path = Path.Combine(rootDirectory, "searchindex.js");
            if (!File.Exists(path))
            {
                return new List<string>();
            }

            var js = File.ReadAllText(path);
            return Regex.Matches(js, "\\[\"([^\"]+\\.html[^\"]*)\"")
                .Cast<Match>()
                .Select(match => match.Groups[1].Value)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static string MakeRelative(string root, string path)
        {
            return path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace(Path.DirectorySeparatorChar, '/');
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CslApiDump
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                return Run(args);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("CslApiDump: " + exception.Message);
                return 1;
            }
        }

        private static int Run(string[] args)
        {
            string root = null;
            string layoutPath = null;
            string documentationPath = null;
            string listType = null;
            var listAll = false;
            var indexOnly = false;

            for (var index = 0; index < args.Length; index++)
            {
                switch (args[index])
                {
                    case "--root":
                        root = args[++index];
                        break;
                    case "--layout":
                        layoutPath = args[++index];
                        break;
                    case "--xml":
                        documentationPath = args[++index];
                        break;
                    case "--list-type":
                        listType = args[++index];
                        break;
                    case "--list-all":
                        listAll = true;
                        break;
                    case "--index":
                        indexOnly = true;
                        break;
                    default:
                        Console.Error.WriteLine("CslApiDump: unknown argument '" + args[index] + "'");
                        return 1;
                }
            }

            if (listAll || listType != null)
            {
                return List(args, listType, listAll);
            }

            if (indexOnly)
            {
                if (root == null)
                {
                    Console.Error.WriteLine("CslApiDump: --index needs --root");
                    return 1;
                }

                SearchIndexBuilder.Build(root);
                return 0;
            }

            if (root == null || layoutPath == null)
            {
                Console.Error.WriteLine("Usage: CslApiDump --root <manual root> --layout <api-layout.json> [--xml <CSLibrary2026.xml>]");
                return 1;
            }

            var layout = ApiLayoutReader.Read(layoutPath);

            if (documentationPath == null)
            {
                documentationPath = FindDocumentationFile();
            }

            Console.WriteLine("Documentation file : " + documentationPath);
            var documentation = new XmlDocumentation(documentationPath);
            Console.WriteLine("Documented members : " + documentation.Count);

            var assembly = typeof(CSLibrary.HighLevelInterface).Assembly;
            Console.WriteLine("Assembly           : " + assembly.GetName().Name);

            var builder = new ApiModelBuilder(documentation);
            var all = builder.Build(assembly, typeName => ObtainHint(layout, typeName));

            var documented = new HashSet<string>(layout.Pages.SelectMany(p => p.Types), StringComparer.Ordinal);
            var included = all.Where(t => documented.Contains(t.FullName)).ToList();

            Console.WriteLine("Public types       : " + all.Count);
            Console.WriteLine("Documented types   : " + included.Count);
            Console.WriteLine("Excluded types     : " + (all.Count - included.Count));

            ReportUnassignedMembers(layout, included);

            var renderer = new PageRenderer(layout, typeName => ObtainHint(layout, typeName));
            renderer.Write(root, included);

            Console.WriteLine("Pages written to   : " + root);
            return 0;
        }

        /// <summary>
        /// Prints the public API of the assembly. Used to maintain the layout
        /// file: it lists the types that the manual can document and the members
        /// of a type.
        /// </summary>
        private static int List(string[] args, string typeName, bool all)
        {
            var assembly = typeof(CSLibrary.HighLevelInterface).Assembly;

            if (all)
            {
                foreach (var type in assembly.GetExportedTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
                {
                    Console.WriteLine(type.FullName);
                }

                return 0;
            }

            var target = assembly.GetExportedTypes()
                .FirstOrDefault(t => string.Equals(t.FullName, typeName, StringComparison.Ordinal) ||
                                     string.Equals(t.Name, typeName, StringComparison.Ordinal));

            if (target == null)
            {
                Console.Error.WriteLine("CslApiDump: the type '" + typeName + "' was not found.");
                return 1;
            }

            Console.WriteLine("TYPE " + target.FullName);

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance |
                                       BindingFlags.Static | BindingFlags.DeclaredOnly;

            foreach (var member in target.GetMembers(flags).OrderBy(m => m.Name, StringComparer.Ordinal))
            {
                var name = member.Name;
                if (member is MethodInfo method && !method.IsSpecialName)
                {
                    Console.WriteLine("  METHOD    " + name);
                }
                else if (member is PropertyInfo)
                {
                    Console.WriteLine("  PROPERTY  " + name);
                }
                else if (member is FieldInfo)
                {
                    Console.WriteLine("  FIELD     " + name);
                }
                else if (member is EventInfo)
                {
                    Console.WriteLine("  EVENT     " + name);
                }
                else if (member is ConstructorInfo)
                {
                    Console.WriteLine("  CTOR      " + target.Name);
                }
            }

            return 0;
        }

        /// <summary>
        /// Warns about the members of a documented type that no member group
        /// mentions, so that a new API member is never silently left out.
        /// </summary>
        private static void ReportUnassignedMembers(ApiLayout layout, IReadOnlyList<ApiType> types)
        {
            var index = types.ToDictionary(t => t.FullName, StringComparer.Ordinal);

            foreach (var page in layout.Pages)
            {
                if (page.MemberGroups.Count == 0 || page.Types.Count == 0)
                {
                    continue;
                }

                ApiType type;
                if (!index.TryGetValue(page.Types[0], out type))
                {
                    continue;
                }

                var assigned = new HashSet<string>(StringComparer.Ordinal);
                foreach (var group in page.MemberGroups)
                {
                    foreach (var member in group.Members)
                    {
                        assigned.Add(member);
                    }
                }

                var ignored = new HashSet<string>(layout.Ignore, StringComparer.Ordinal);

                var missing = type.Members
                    .Where(m => !assigned.Contains(m.Name))
                    .Where(m => !ignored.Contains(m.Name))
                    .Where(m => !m.Name.EndsWith("_CS108", StringComparison.Ordinal))
                    .Where(m => !m.Name.EndsWith("_CS710S", StringComparison.Ordinal))
                    .Select(m => m.Name)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(n => n, StringComparer.Ordinal)
                    .ToList();

                if (missing.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("WARNING: the page '" + page.Title +
                                      "' does not document: " + string.Join(", ", missing));
                }
            }
        }

        private static string ObtainHint(ApiLayout layout, string typeName)
        {
            string hint;
            if (layout.Obtain.TryGetValue(typeName, out hint))
            {
                return hint;
            }

            return string.Empty;
        }

        /// <summary>
        /// Looks for the XML documentation file next to the compiled library.
        /// </summary>
        private static string FindDocumentationFile()
        {
            var assemblyLocation = typeof(CSLibrary.HighLevelInterface).Assembly.Location;
            var directory = Path.GetDirectoryName(assemblyLocation);
            var candidate = Path.Combine(directory ?? ".", "CSLibrary2026.xml");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            throw new FileNotFoundException(
                "The XML documentation file was not found next to the assembly. " +
                "Build the library with -p:GenerateDocumentationFile=true or pass --xml.",
                candidate);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CslApiDump
{
    /// <summary>
    /// Describes the pages of the API reference and which types belong to them.
    /// </summary>
    /// <remarks>
    /// The layout is the single place that decides what the manual contains. It
    /// keeps the application API (the types an application developer calls)
    /// separate from the internal types that happen to be public in the
    /// assembly.
    /// </remarks>
    internal sealed class ApiLayout
    {
        public string Title { get; set; } = "CSLibrary2026 API Reference";
        public string Subtitle { get; set; } = string.Empty;
        public List<ApiPage> Pages { get; set; } = new List<ApiPage>();

        /// <summary>
        /// Describes how an application obtains an instance of a type, for
        /// example <c>reader.rfid</c> for a subsystem or
        /// <c>new TagReadParms { ... }</c> for a parameter structure.
        /// </summary>
        public Dictionary<string, string> Obtain { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Members that are public but deliberately not part of the manual, for
        /// example the per model dispatch helpers and the raw protocol members.
        /// </summary>
        public List<string> Ignore { get; set; } = new List<string>();
    }

    internal sealed class ApiPage
    {
        /// <summary>Page identifier, used for the anchor and the file name.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Title shown in the navigation and at the top of the page.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Path of the generated page, relative to the manual root.</summary>
        public string File { get; set; } = string.Empty;

        /// <summary>Short introduction shown below the title.</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>Fully qualified names of the types documented on the page.</summary>
        public List<string> Types { get; set; } = new List<string>();

        /// <summary>
        /// True when the page is written by hand. Such a page appears in the
        /// navigation but is not generated.
        /// </summary>
        public bool Handwritten { get; set; }

        /// <summary>Optional grouping of the members of a single type.</summary>
        public List<ApiMemberGroup> MemberGroups { get; set; } = new List<ApiMemberGroup>();
    }

    /// <summary>
    /// A named group of members of one type. Used to split a large type (such as
    /// RFIDReader) over several sections of a page.
    /// </summary>
    internal sealed class ApiMemberGroup
    {
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<string> Members { get; set; } = new List<string>();
    }

    internal static class ApiLayoutReader
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        public static ApiLayout Read(string path)
        {
            string json = File.ReadAllText(path);
            var layout = JsonSerializer.Deserialize<ApiLayout>(json, Options);
            if (layout == null)
            {
                throw new InvalidOperationException("The layout file could not be read: " + path);
            }

            if (layout.Pages.Count == 0)
            {
                throw new InvalidOperationException("The layout does not define any page: " + path);
            }

            foreach (var page in layout.Pages)
            {
                if (string.IsNullOrEmpty(page.Id) || string.IsNullOrEmpty(page.Title) || string.IsNullOrEmpty(page.File))
                {
                    throw new InvalidOperationException("A page of the layout is missing its id, title or file: " + path);
                }
            }

            return layout;
        }
    }
}

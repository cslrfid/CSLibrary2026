using System;
using System.Collections.Generic;
using System.Text;

namespace CslApiDump
{
    /// <summary>
    /// Small helpers for building the HTML pages.
    /// </summary>
    internal static class Html
    {
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length + 16);
            foreach (var character in value)
            {
                switch (character)
                {
                    case '&': builder.Append("&amp;"); break;
                    case '<': builder.Append("&lt;"); break;
                    case '>': builder.Append("&gt;"); break;
                    case '"': builder.Append("&quot;"); break;
                    case '\'': builder.Append("&#39;"); break;
                    default: builder.Append(character); break;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Escapes a value for use inside an HTML code element, keeping the
        /// generic brackets readable.
        /// </summary>
        public static string Code(string value)
        {
            return "<code>" + Escape(value) + "</code>";
        }

        /// <summary>
        /// Turns a plain text description into HTML. The documentation comments
        /// only contain plain text, so a single pass is enough.
        /// </summary>
        public static string Paragraph(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return "<p>" + Escape(value) + "</p>";
        }

        public static string Anchor(string id)
        {
            return "id=\"" + Escape(id) + "\"";
        }
    }
}

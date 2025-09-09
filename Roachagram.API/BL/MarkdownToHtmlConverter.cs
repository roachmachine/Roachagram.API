using Markdig;

namespace Roachagram.API.BL
{
    public static class MarkdownToHtmlConverter
    {
        /// <summary>
        /// Converts a Markdown string to HTML.
        /// </summary>
        /// <param name="markdown">The Markdown string to convert.</param>
        /// <returns>The converted HTML string.</returns>
        public static string ConvertToHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return string.Empty;
            }

            // Use Markdig library to convert Markdown to HTML
            return Markdown.ToHtml(markdown);
        }
    }
}

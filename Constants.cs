using System.Text.RegularExpressions;

namespace UpnoteToObsidian;

/// <summary>
/// Constants used throughout the application for parsing and content processing.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The separator used in category paths to denote hierarchy levels.
    /// </summary>
    public const string CategoryPathSeparator = " / ";
    
    /// <summary>
    /// Regular expressions for parsing markdown metadata.
    /// </summary>
    public static class Regex
    {
        public static readonly System.Text.RegularExpressions.Regex DateRegex = 
            new(@"date:\s*(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})", RegexOptions.Compiled);
            
        public static readonly System.Text.RegularExpressions.Regex CreatedRegex = 
            new(@"created:\s*(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})", RegexOptions.Compiled);
            
        public static readonly System.Text.RegularExpressions.Regex CategoriesRegex = 
            new(@"categories:\s*\n((?:- .+\n?)+)", RegexOptions.Compiled | RegexOptions.Multiline);
            
        public static readonly System.Text.RegularExpressions.Regex CategoryLineRegex = 
            new(@"- (.+)", RegexOptions.Compiled);

        /// <summary>
        /// Patterns for matching asset references in markdown content.
        /// </summary>
        public static readonly string[] AssetReferencePatterns = 
        {
            @"!\[[^\]]*\]\(([^)]+)\)",           // ![alt](path)
            @"!\[[^\]]*\]\[\s*([^\]]+)\s*\]",    // ![alt][ref]
            @"\[([^\]]+)\]:\s*([^\s]+)",         // [ref]: path
            @"<img[^>]+src=[""']([^""']+)[""']", // <img src="path">
            @"<a[^>]+href=[""']([^""']+)[""']"   // <a href="path">
        };
    }

    /// <summary>
    /// HTML tags to be removed from markdown content.
    /// </summary>
    public static readonly Dictionary<string, string> HtmlTagReplacements = new()
    {
        { "<em>", "" }, { "</em>", "" },
        { "<strong>", "" }, { "</strong>", "" },
        { "<u>", "" }, { "</u>", "" },
        { "<s>", "" }, { "</s>", "" },
        { "<code>", "" }, { "</code>", "" },
        { "<mark>", "" }, { "</mark>", "" },
        { "<br>", "" }, { "<br/>", "" }, { "<br />", "" }
    };

    /// <summary>
    /// HTML entities to be converted.
    /// </summary>
    public static readonly Dictionary<string, string> HtmlEntityReplacements = new()
    {
        { "&lt;", "<" },
        { "&gt;", ">" },
        { "&amp;", "&" },
        { "&nbsp;", " " }
    };

    /// <summary>
    /// LaTeX commands to be removed.
    /// </summary>
    public static readonly string[] LaTeXCommandsToRemove = 
    {
        @"\\newpage"
    };
}

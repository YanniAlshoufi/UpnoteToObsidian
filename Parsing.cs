using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using UpnoteToObsidian.Models;

namespace UpnoteToObsidian;

/// <summary>
/// Contains functions for parsing markdown files and processing their content.
/// </summary>
public static class Parsing
{
    /// <summary>
    /// Parses a markdown file from the file system.
    /// </summary>
    /// <param name="filePath">The path to the markdown file.</param>
    /// <returns>A Result containing the parsed MarkdownFile or an error message.</returns>
    public static Result<MarkdownFile> ParseMarkdownFile(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            return ParseMarkdownContent(filePath, content);
        }
        catch (Exception ex)
        {
            return Result.Failure<MarkdownFile>($"Failed to read file: {filePath}. Error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Parses markdown content from a string.
    /// </summary>
    /// <param name="filePath">The original file path for reference.</param>
    /// <param name="content">The markdown content to parse.</param>
    /// <returns>A Result containing the parsed MarkdownFile or an error message.</returns>
    public static Result<MarkdownFile> ParseMarkdownContent(string filePath, string content)
    {
        var fileName = Path.GetFileName(filePath);
        
        var dateResult = ExtractDate(content);
        var createdResult = ExtractCreated(content);
        var categoriesResult = ExtractCategories(content);
        
        return Result.Combine(dateResult, createdResult, categoriesResult)
            .Map(() => new MarkdownFile(
                filePath,
                fileName,
                dateResult.Value,
                createdResult.Value,
                categoriesResult.Value,
                content
            ));
    }
    
    private static Result<DateTime> ExtractDate(string content)
    {
        var match = Constants.Regex.DateRegex.Match(content);
        if (!match.Success)
        {
            return Result.Failure<DateTime>("Date not found in markdown file");
        }
            
        return DateTime.TryParse(match.Groups[1].Value, out var date)
            ? Result.Success(date)
            : Result.Failure<DateTime>($"Invalid date format: {match.Groups[1].Value}");
    }
    
    private static Result<DateTime> ExtractCreated(string content)
    {
        var match = Constants.Regex.CreatedRegex.Match(content);
        if (!match.Success)
        {
            return Result.Failure<DateTime>("Created date not found in markdown file");
        }
            
        return DateTime.TryParse(match.Groups[1].Value, out var created)
            ? Result.Success(created)
            : Result.Failure<DateTime>($"Invalid created date format: {match.Groups[1].Value}");
    }
    
    private static Result<IReadOnlyList<string>> ExtractCategories(string content)
    {
        var match = Constants.Regex.CategoriesRegex.Match(content);
        if (!match.Success)
        {
            return Result.Failure<IReadOnlyList<string>>("Categories not found in markdown file");
        }
            
        var categoriesText = match.Groups[1].Value;
        var categories = Constants.Regex.CategoryLineRegex.Matches(categoriesText)
            .Cast<Match>()
            .Select(m => m.Groups[1].Value.Trim())
            .ToList();
            
        return categories.Any()
            ? Result.Success<IReadOnlyList<string>>(categories.AsReadOnly())
            : Result.Failure<IReadOnlyList<string>>("No categories found");
    }
    
    /// <summary>
    /// Processes markdown content by removing HTML tags, fixing LaTeX commands, 
    /// and normalizing backslashes and dollar signs.
    /// </summary>
    /// <param name="content">The markdown content to process.</param>
    /// <returns>The processed markdown content.</returns>
    public static string ProcessMarkdownContent(string content)
    {
        // Replace HTML entities
        foreach (var replacement in Constants.HtmlEntityReplacements)
        {
            content = content.Replace(replacement.Key, replacement.Value);
        }
        
        // Remove HTML tags
        foreach (var replacement in Constants.HtmlTagReplacements)
        {
            content = content.Replace(replacement.Key, replacement.Value);
        }
        
        // Remove other HTML tags
        content = Regex.Replace(content, @"<[^>]+>", "", RegexOptions.IgnoreCase);
        
        // Remove LaTeX commands
        foreach (var command in Constants.LaTeXCommandsToRemove)
        {
            content = Regex.Replace(content, command, "", RegexOptions.IgnoreCase);
        }
        
        // Replace double backslashes with single backslashes
        content = content.Replace(@"\\", @"\");
        
        // Replace double dollar signs with single dollar signs
        content = content.Replace("$$", "$");
        
        // Trim whitespace inside LaTeX expressions ($...$)
        content = TrimLatexWhitespace(content);
        
        // Remove backslashes that are immediately followed by something that is not a letter, percent sign, or equals sign
        content = Regex.Replace(content, @"\\(?![a-zA-Z%=])", "");
        
        return content;
    }
    
    /// <summary>
    /// Trims whitespace immediately after the opening $ and immediately before the closing $ in LaTeX expressions.
    /// </summary>
    /// <param name="content">The content containing LaTeX expressions.</param>
    /// <returns>The content with trimmed LaTeX expressions.</returns>
    private static string TrimLatexWhitespace(string content)
    {
        // Use regex to find $...$ patterns and trim whitespace inside them
        return Regex.Replace(content, @"\$([^$]*)\$", match =>
        {
            var innerContent = match.Groups[1].Value;
            var trimmedContent = innerContent.Trim();
            return $"${trimmedContent}$";
        });
    }

    public static Result<NotebookNode> BuildNotebookStructure(string notebooksPath)
    {
        try
        {
            if (!Directory.Exists(notebooksPath))
            {
                return Result.Success(new NotebookNode("root", notebooksPath, new List<NotebookNode>()));
            }
                
            return Result.Success(BuildNotebookNode(notebooksPath, "root"));
        }
        catch (Exception ex)
        {
            return Result.Failure<NotebookNode>($"Failed to build notebook structure from: {notebooksPath}. Error: {ex.Message}");
        }
    }
    
    private static NotebookNode BuildNotebookNode(string path, string name)
    {
        var children = new List<NotebookNode>();
        
        if (Directory.Exists(path))
        {
            foreach (var directory in Directory.GetDirectories(path))
            {
                var dirName = Path.GetFileName(directory);
                children.Add(BuildNotebookNode(directory, dirName));
            }
        }
        
        return new NotebookNode(name, path, children.AsReadOnly());
    }
    
    public static Result<string> FindNotebookPathForFile(MarkdownFile file, NotebookNode rootNode)
    {
        if (!file.Categories.Any())
        {
            return Result.Failure<string>("File has no categories");
        }
            
        // Take the last category as it's usually the most specific
        var targetCategory = file.Categories.Last();
        
        // Split the category path by the separator to get the hierarchy
        var pathParts = targetCategory.Split(Constants.CategoryPathSeparator, StringSplitOptions.RemoveEmptyEntries);
        
        return FindNodeByHierarchy(rootNode, pathParts, 0)
            .Map(node => node.Path); // Return the category path, not a file system path
    }
    
    private static Result<NotebookNode> FindNodeByHierarchy(NotebookNode node, string[] pathParts, int currentIndex)
    {
        if (currentIndex >= pathParts.Length)
        {
            return Result.Success(node);
        }
        
        var targetPart = pathParts[currentIndex];
        
        // First try exact match
        foreach (var child in node.Children)
        {
            if (child.Name.Equals(targetPart, StringComparison.OrdinalIgnoreCase))
            {
                return FindNodeByHierarchy(child, pathParts, currentIndex + 1);
            }
        }
        
        // Try fuzzy matching - normalize both strings by removing special characters and extra spaces
        var normalizedTarget = NormalizeForMatching(targetPart);
        
        foreach (var child in node.Children)
        {
            var normalizedChild = NormalizeForMatching(child.Name);
            
            if (normalizedChild.Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase))
            {
                return FindNodeByHierarchy(child, pathParts, currentIndex + 1);
            }
        }
        
        // Try partial matching
        foreach (var child in node.Children)
        {
            var normalizedChild = NormalizeForMatching(child.Name);
            
            if (normalizedChild.Contains(normalizedTarget, StringComparison.OrdinalIgnoreCase) || 
                normalizedTarget.Contains(normalizedChild, StringComparison.OrdinalIgnoreCase))
            {
                return FindNodeByHierarchy(child, pathParts, currentIndex + 1);
            }
        }
        
        return Result.Failure<NotebookNode>($"Node not found: {string.Join(" / ", pathParts)} (failed at: {targetPart})");
    }
    
    private static string NormalizeForMatching(string input)
    {
        return input
            .Replace(":", "")
            .Replace(",", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("–", "-")
            .Replace("−", "-")
            .Trim()
            .Replace("  ", " ")
            .Replace("  ", " "); // Run twice to handle multiple spaces
    }
}

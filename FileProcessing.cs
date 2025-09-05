using CSharpFunctionalExtensions;
using UpnoteToObsidian.Models;
using System.Text.RegularExpressions;

namespace UpnoteToObsidian;

public static class FileProcessing
{
    public static Result<IReadOnlyList<MarkdownFile>> GetAllMarkdownFiles(string inputFolder)
    {
        try
        {
            if (!Directory.Exists(inputFolder))
            {
                return Result.Success<IReadOnlyList<MarkdownFile>>(new List<MarkdownFile>().AsReadOnly());
            }
                
            var markdownFiles = Directory.GetFiles(inputFolder, "*.md", SearchOption.TopDirectoryOnly);
            var results = new List<MarkdownFile>();
            var errors = new List<string>();
            
            foreach (var filePath in markdownFiles)
            {
                var parseResult = Parsing.ParseMarkdownFile(filePath);
                if (parseResult.IsSuccess)
                {
                    results.Add(parseResult.Value);
                }
                else
                {
                    errors.Add($"Failed to parse {filePath}: {parseResult.Error}");
                }
            }
            
            if (errors.Any())
            {
                Console.WriteLine($"Warnings: {string.Join("; ", errors)}");
            }
                
            return Result.Success<IReadOnlyList<MarkdownFile>>(results.AsReadOnly());
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<MarkdownFile>>($"Failed to get markdown files from: {inputFolder}. Error: {ex.Message}");
        }
    }
    
    public static Result ProcessInputFolder(string inputFolder, string outputFolder)
    {
        var folderName = Path.GetFileName(inputFolder);
        var outputPath = Path.Combine(outputFolder, folderName);
        
        return GetAllMarkdownFiles(inputFolder)
            .Bind(markdownFiles => ProcessMarkdownFiles(markdownFiles, inputFolder, outputPath))
            .Bind(() => CopyAssetsToDirectories(inputFolder, outputPath));
    }
    
    private static Result<NotebookNode> CreateNotebookStructureFromCategories(IReadOnlyList<MarkdownFile> markdownFiles)
    {
        try
        {
            // Collect all unique categories
            var allCategories = new HashSet<string>();
            foreach (var file in markdownFiles)
            {
                foreach (var category in file.Categories)
                {
                    allCategories.Add(category);
                }
            }
            
            // Build a tree structure from the categories
            var nodeMap = new Dictionary<string, NotebookNode>();
            
            foreach (var category in allCategories)
            {
                var pathParts = category.Split(Constants.CategoryPathSeparator, StringSplitOptions.RemoveEmptyEntries);
                
                for (int i = 0; i < pathParts.Length; i++)
                {
                    var currentPartialPath = string.Join(Constants.CategoryPathSeparator, pathParts.Take(i + 1));
                    
                    if (!nodeMap.ContainsKey(currentPartialPath))
                    {
                        var nodeName = pathParts[i];
                        var children = new List<NotebookNode>();
                        nodeMap[currentPartialPath] = new NotebookNode(nodeName, currentPartialPath, children.AsReadOnly());
                    }
                }
            }
            
            // Now build the parent-child relationships
            foreach (var category in allCategories)
            {
                var pathParts = category.Split(Constants.CategoryPathSeparator, StringSplitOptions.RemoveEmptyEntries);
                
                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    var parentPath = string.Join(Constants.CategoryPathSeparator, pathParts.Take(i + 1));
                    var childPath = string.Join(Constants.CategoryPathSeparator, pathParts.Take(i + 2));
                    
                    var parentNode = nodeMap[parentPath];
                    var childNode = nodeMap[childPath];
                    
                    // Add child to parent if not already added
                    if (!parentNode.Children.Any(c => c.Path == childPath))
                    {
                        var updatedChildren = parentNode.Children.ToList();
                        updatedChildren.Add(childNode);
                        nodeMap[parentPath] = parentNode with { Children = updatedChildren.AsReadOnly() };
                    }
                }
            }
            
            // Create root node with all top-level categories as children
            var rootChildren = new List<NotebookNode>();
            foreach (var category in allCategories)
            {
                var topLevelPath = category.Split(Constants.CategoryPathSeparator, StringSplitOptions.RemoveEmptyEntries)[0];
                var topLevelNode = nodeMap[topLevelPath];
                if (!rootChildren.Any(c => c.Path == topLevelPath))
                {
                    rootChildren.Add(topLevelNode);
                }
            }
            
            var rootNode = new NotebookNode("root", "", rootChildren.AsReadOnly());
            return Result.Success(rootNode);
        }
        catch (Exception ex)
        {
            return Result.Failure<NotebookNode>($"Failed to create notebook structure from categories: {ex.Message}");
        }
    }

    private static Result ProcessMarkdownFiles(IReadOnlyList<MarkdownFile> markdownFiles, string inputFolder, string outputPath)
    {
        // Build notebook structure from categories found in files instead of from a directory
        return CreateNotebookStructureFromCategories(markdownFiles)
            .Bind(notebookStructure => ProcessFilesWithStructure(markdownFiles, notebookStructure, outputPath));
    }
    
    private static Result ProcessFilesWithStructure(IReadOnlyList<MarkdownFile> markdownFiles, NotebookNode notebookStructure, string outputPath)
    {
        var results = new List<Result>();
        
        foreach (var file in markdownFiles)
        {
            var result = ProcessSingleFile(file, notebookStructure, outputPath);
            results.Add(result);
            
            if (result.IsFailure)
            {
                Console.WriteLine($"Warning: {result.Error}");
            }
        }
        
        return Result.Combine(results.ToArray());
    }
    
    private static Result ProcessSingleFile(MarkdownFile file, NotebookNode notebookStructure, string outputPath)
    {
        if (!file.Categories.Any())
        {
            return Result.Failure("File has no categories");
        }
        
        // Use the last category as the target path (most specific)
        var targetCategory = file.Categories.Last();
        
        return CreateOutputFileStructure(file, targetCategory, outputPath)
            .Bind(targetPath => WriteProcessedFile(file, targetPath));
    }
    
    private static Result<string> CreateOutputFileStructure(MarkdownFile file, string categoryPath, string outputPath)
    {
        try
        {
            // Split the category path by the separator to get the hierarchy
            var pathParts = categoryPath.Split(Constants.CategoryPathSeparator, StringSplitOptions.RemoveEmptyEntries);
            
            // Clean each part individually and replace forward slashes with underscores
            var cleanedParts = pathParts.Select(part => {
                return part
                    .Replace("/", "_")  // Replace forward slashes with underscores
                    .Replace(":", "_")
                    .Replace("?", "_")
                    .Replace("*", "_")
                    .Replace("<", "_")
                    .Replace(">", "_")
                    .Replace("|", "_")
                    .Replace("\"", "_");
            }).ToArray();
            
            var targetDir = Path.Combine(new[] { outputPath }.Concat(cleanedParts).ToArray());
            
            Directory.CreateDirectory(targetDir);
            
            return Result.Success(Path.Combine(targetDir, file.FileName));
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Failed to create output structure for file: {file.FileName}. Error: {ex.Message}");
        }
    }
    
    private static Result WriteProcessedFile(MarkdownFile file, string targetPath)
    {
        try
        {
            var processedContent = Parsing.ProcessMarkdownContent(file.Content);
            File.WriteAllText(targetPath, processedContent);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to write processed file to: {targetPath}. Error: {ex.Message}");
        }
    }
    
    private static Result CopyAssetsToDirectories(string inputFolder, string outputPath)
    {
        try
        {
            var assetsPath = Path.Combine(inputFolder, "Files");
            if (!Directory.Exists(assetsPath))
            {
                return Result.Success();
            }
            
            // Process all directories and copy only referenced assets
            CopyReferencedAssetsToAllDirectories(outputPath, assetsPath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to copy assets from: {inputFolder}. Error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Extracts asset references from markdown content using various patterns.
    /// </summary>
    /// <param name="markdownContent">The markdown content to analyze.</param>
    /// <returns>A set of asset filenames referenced in the content.</returns>
    internal static HashSet<string> ExtractAssetReferences(string markdownContent)
    {
        var assetReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var pattern in Constants.Regex.AssetReferencePatterns)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var matches = regex.Matches(markdownContent);
            
            foreach (Match match in matches)
            {
                // Get the path from the appropriate capture group
                var path = match.Groups[1].Value;
                if (match.Groups.Count > 2 && !string.IsNullOrEmpty(match.Groups[2].Value))
                {
                    path = match.Groups[2].Value;
                }
                
                // Extract just the filename from the path
                var fileName = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(fileName))
                {
                    // URL decode the filename to handle %20 -> space conversions
                    try
                    {
                        fileName = Uri.UnescapeDataString(fileName);
                    }
                    catch
                    {
                        // If URL decoding fails, use the original filename
                    }
                    
                    assetReferences.Add(fileName);
                }
            }
        }
        
        return assetReferences;
    }
    
    private static void CopyReferencedAssetsToAllDirectories(string outputPath, string assetsPath)
    {
        // Get all markdown files directly in this directory (not in subdirectories)
        var markdownFiles = Directory.GetFiles(outputPath, "*.md", SearchOption.TopDirectoryOnly);
        
        if (markdownFiles.Any())
        {
            // Collect all asset references from markdown files in this directory
            var referencedAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var markdownFile in markdownFiles)
            {
                try
                {
                    var content = File.ReadAllText(markdownFile);
                    var assetRefs = ExtractAssetReferences(content);
                    foreach (var assetRef in assetRefs)
                    {
                        referencedAssets.Add(assetRef);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to read markdown file {markdownFile}: {ex.Message}");
                }
            }
            
            // Copy only the referenced assets to this directory's Files folder
            if (referencedAssets.Any())
            {
                var filesDir = Path.Combine(outputPath, "Files");
                Directory.CreateDirectory(filesDir);
                CopySpecificAssets(assetsPath, filesDir, referencedAssets);
            }
        }
        
        // Recursively process all subdirectories
        foreach (var directory in Directory.GetDirectories(outputPath))
        {
            CopyReferencedAssetsToAllDirectories(directory, assetsPath);
        }
    }
    
    private static void CopySpecificAssets(string sourceDir, string destDir, HashSet<string> assetNames)
    {
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }
            
        // Copy only the files that are referenced
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            if (assetNames.Contains(fileName))
            {
                var destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, true);
            }
        }
        
        // Also check subdirectories for assets
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopySpecificAssets(dir, destDir, assetNames);
        }
    }
    
    public static Result ProcessAllInputFolders(string inputRootPath, string outputRootPath)
    {
        try
        {
            if (!Directory.Exists(inputRootPath))
            {
                return Result.Failure($"Input path does not exist: {inputRootPath}");
            }
                
            Directory.CreateDirectory(outputRootPath);
            
            var inputFolders = Directory.GetDirectories(inputRootPath);
            var results = new List<Result>();
            
            foreach (var inputFolder in inputFolders)
            {
                Console.WriteLine($"Processing folder: {Path.GetFileName(inputFolder)}");
                var result = ProcessInputFolder(inputFolder, outputRootPath);
                results.Add(result);
                
                if (result.IsSuccess)
                {
                    Console.WriteLine($"✅ Successfully processed: {Path.GetFileName(inputFolder)}");
                }
                else
                {
                    Console.WriteLine($"❌ Failed to process: {Path.GetFileName(inputFolder)} - {result.Error}");
                }
            }
            
            return Result.Combine(results.ToArray());
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to process input folders from: {inputRootPath}. Error: {ex.Message}");
        }
    }
}

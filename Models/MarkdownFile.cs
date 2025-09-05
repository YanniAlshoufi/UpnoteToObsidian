using CSharpFunctionalExtensions;

namespace UpnoteToObsidian.Models;

public record MarkdownFile(
    string FilePath,
    string FileName,
    DateTime Date,
    DateTime Created,
    IReadOnlyList<string> Categories,
    string Content
);

public record NotebookNode(
    string Name,
    string Path,
    IReadOnlyList<NotebookNode> Children
);

public record ProcessedFile(
    MarkdownFile Original,
    string ProcessedContent,
    string TargetPath
);
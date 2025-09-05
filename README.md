# UpnoteToObsidian Converter

Converts UpNote markdown exports to Obsidian-compatible format with automatic directory structure and selective asset copying.

## Quick Start

```bash
# Place UpNote exports in input/ directory
dotnet run
# Find converted files in output/ directory
```

## Features

- **Content Processing**: Removes HTML tags, converts entities, fixes LaTeX/math notation
- **Smart Assets**: Only copies referenced files with URL decoding support
- **Cross-Platform**: Safe filename character replacement (`/` → `_`, `:` → `_`, etc.)
- **Functional Design**: Result pattern error handling, immutable records

## Input Format

```
input/FolderName/
├── Files/           # Assets (images, docs)
├── *.md            # Markdown files with categories metadata
```

**Markdown Example:**
```markdown
---
categories:
- Matura / Physik / (III/IV) A 1 Mechanik
---
# Content with ![image](Files/photo.png)
```

## Output

Creates `output/FolderName/` with:
- Directory structure from categories: `Matura/Physik/(III_IV) A 1 Mechanik/`
- Processed markdown files with cleaned content
- `Files/` folders containing only referenced assets

## Dependencies

- .NET 9, OneOf 3.0.271, CSharpFunctionalExtensions 3.6.0

## Architecture

Functional programming with records, static methods, Result<T> pattern. No inheritance.

**Files:**
- `Models/MarkdownFile.cs` - Data models
- `Constants.cs` - Regex patterns and constants  
- `Parsing.cs` - Content processing
- `FileProcessing.cs` - File operations
- `Program.cs` - Entry point

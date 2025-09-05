# UpnoteToObsidian Converter - Updated Specifications

## Introduction

This document outlines the complete specifications for the UpnoteToObsidian converter, incorporating all implementation details and changes made during development.

## Input

In the "input" folder, there are 0 to many folders, each with this structure:

- **Files/** (containing assets like images, documents, etc.)
- **notebooks/** (containing a hierarchical notebook structure)
- **\*.md** (all the markdown files with no order and TOP LEVEL - no subfolders)

Each markdown file contains a YAML frontmatter header with metadata including:
- **categories**: Exact notebook path that the markdown belongs to (matches notebook tree structure in "notebooks" folder)
- **date**: Creation or modification timestamp
- **created**: Initial creation timestamp

## Requirements

An "output" folder with a folder per folder in the "input" folder. Each folder in output contains the corresponding processed output for an input folder.

The output contains:

### Directory Structure
- The notebooks tree structure with corresponding markdown files placed inside appropriate directories
- Directory structure matches the category paths from markdown file metadata
- **Character Normalization**: Replace invalid filesystem characters with underscores:
  - Forward slashes (`/`) → underscores (`_`) for cross-platform compatibility
  - Colons (`:`) → underscores (`_`)
  - Question marks (`?`) → underscores (`_`)
  - Asterisks (`*`) → underscores (`_`)
  - Angle brackets (`<`, `>`) → underscores (`_`)
  - Pipe symbols (`|`) → underscores (`_`)
  - Double quotes (`"`) → underscores (`_`)
- **Category Path Separation**: Uses `" / "` (space-slash-space) as the standard separator for category hierarchies

### Content Processing
- **Preserve Headers**: Do NOT delete any YAML frontmatter headers - all metadata remains in resulting files
- **HTML Tag Removal**: Remove HTML tags including:
  - `<em>`, `</em>`
  - `<strong>`, `</strong>`
  - `<u>`, `</u>`
  - `<s>`, `</s>`
  - `<code>`, `</code>`
  - `<mark>`, `</mark>`
  - `<br>`, `<br/>`, `<br />`
- **HTML Entity Conversion**: Convert HTML entities:
  - `&lt;` → `<`
  - `&gt;` → `>`
  - `&amp;` → `&`
  - `&nbsp;` → ` ` (space)
- **LaTeX Command Removal**: Remove `\newpage` and similar LaTeX commands
- **Backslash Normalization**: Replace double backslashes (`\\`) with single backslashes (`\`)
- **Dollar Sign Normalization**: Replace double dollar signs (`$$`) with single dollar signs (`$`)
- **LaTeX Whitespace Trimming**: Remove whitespace immediately after opening `$` and immediately before closing `$` in LaTeX expressions for proper rendering
- **Invalid Backslash Removal**: Remove backslashes immediately followed by non-letter, non-percent, non-equals characters

### Asset Management
- **Selective Copying**: Copy only assets that are actually referenced by markdown files in each specific directory
- **Directory-Specific Assets**: Each directory containing markdown files gets its own "Files" folder with only the assets referenced by those specific markdown files
- **URL Decoding**: Handle URL-encoded asset filenames (e.g., `image%206.png` → `image 6.png`)
- **Reference Pattern Support**: Support various asset reference formats:
  - `![alt](Files/asset.png)` - Standard markdown images
  - `![alt][ref]` - Reference-style links  
  - `<img src="Files/asset.png">` - HTML img tags
  - `[link](Files/document.pdf)` - File links

## Technical Implementation

### Technology Stack
- **C# (.NET 9)**: Latest .NET framework with modern C# features
- **OneOf** (NuGet package): Union types for functional programming patterns
- **CSharpFunctionalExtensions** (NuGet package): Result pattern for proper error handling
- **Cross-Platform Compatibility**: Uses `Path.Combine` and proper path handling for Windows, Linux, and macOS

### Programming Paradigm
- **Functional Programming Style**: Primary approach throughout the application
- **No Inheritance**: Avoid class inheritance in favor of composition
- **Records and Static Methods**: Use records for data and static methods for operations
- **Ref Structs**: May be used where performance is critical (though likely not needed)
- **Result Pattern**: Comprehensive error handling using `Result<T>` instead of exceptions

### Project Structure
- **Solution File**: Organized with proper .sln file containing main project
- **Constants**: Centralized configuration for regex patterns, replacements, normalizations, and the category path separator (`CategoryPathSeparator = " / "`)
- **Modular Design**: Separate files for parsing, file processing, and models
- **Documentation**: Comprehensive XML documentation on all public methods
- **Cross-Platform Path Handling**: Safe filename character replacement for all operating systems

## Architecture Patterns

### Error Handling
- Use `Result<T>` pattern from CSharpFunctionalExtensions for all operations that can fail
- Provide meaningful error messages with context
- Handle file system errors gracefully
- Continue processing other files when individual files fail

### Data Models
- **MarkdownFile**: Immutable record containing file metadata and content
- **NotebookNode**: Hierarchical structure representing notebook organization
- **ProcessedFile**: Result of processing operations

### Processing Pipeline
1. **Discovery**: Find all markdown files in input directories
2. **Parsing**: Extract metadata and content from each markdown file
3. **Category-Based Structure**: Build directory hierarchy directly from category paths in markdown files
4. **Path Processing**: Clean category paths and replace invalid filesystem characters with underscores
5. **Content Processing**: Clean and normalize markdown content
6. **Asset Analysis**: Determine which assets are referenced by which files
7. **Output Generation**: Create directory structure and copy processed files
8. **Selective Asset Copying**: Copy only referenced assets to appropriate locations

## Workflow Implementation

The development followed these principles from the original specifications:

## Key Implementation Features

### Character Normalization for Cross-Platform Compatibility
Converts invalid filesystem characters to underscores to ensure folder names work across Windows, Linux, and macOS:
- `(I/II)` becomes `(I_II)`
- `(III/IV)` becomes `(III_IV)` 
- `Topic: Details` becomes `Topic_ Details`
- Uses consistent `" / "` separator for category path parsing

### Asset Reference Extraction
Sophisticated regex-based system for finding asset references in markdown content, with support for:
- Multiple markdown syntax variations
- URL-encoded filenames
- HTML img tags
- Link references

### Category-Based Directory Creation
Directly uses category metadata from markdown files to create directory structure, eliminating dependency on external notebook folder structure.

### Functional Programming Implementation
- Pure functions where possible
- Immutable data structures
- Composition over inheritance
- Explicit error handling through Result types

## Usage

```bash
# Build and run
dotnet build
dotnet run

# Input structure expected in ./input/
# Output generated in ./output/
```

## Success Criteria

The implementation successfully:
- ✅ Converts UpNote exports to Obsidian-compatible format
- ✅ Preserves all metadata headers
- ✅ Properly cleans HTML tags and entities
- ✅ Normalizes LaTeX and mathematical notation with proper whitespace trimming
- ✅ Creates correct directory hierarchy based on categories
- ✅ Handles cross-platform filesystem compatibility with safe character replacement
- ✅ Uses centralized constants for consistent category path processing
- ✅ Copies only referenced assets to appropriate locations
- ✅ Handles URL-encoded filenames correctly
- ✅ Uses functional programming principles throughout
- ✅ Provides comprehensive error handling
- ✅ Maintains high code quality with proper documentation

This updated specification reflects the complete, working implementation of the UpnoteToObsidian converter as built and tested, including LaTeX whitespace trimming for optimal Obsidian rendering.
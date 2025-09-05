# Introduction

## Input

In the "input" folder, there 0 to many folders, each with this structure:

- File/ (containing assets)
- notebooks/ (containing a notebook structure)
- ... (all the markdown files with no order and TOP LEVEL (no subfolders))

Each markdown file contains a header with "categories" inside of it (usually line 3 and 4) and it has a the exact notebook (notebook tree can be found in the "notebooks" folder) that the markdown belongs in.

Each markdown file also contains at least a "date" and a "created" header.

## Requirements

An "output" folder with a folder per folder in the "input" folder. Each folder in output contains the corresponding output for an input folder.

The output should contain:

- The notebooks tree but with the corresponding markdown files inside of it (for each output folder)
    - Do NOT delete any headers, I want all of them to still be in the resulting files
    - Remove all html tags like "br" and all "newpage" latex commands
    - In all the markdown files, all double backslashes should be replaced by single backslashes
    - In all the markdown files, all double dollar signs should be replaced by single dollar signs
    - In all the markdown files, all backslashes that are immediately followed by something that is not a letter (case insensitive), a percent sign, or an equals sign should be removed
    - Make sure to also move the assets to the correct location in the output folder. It is best to put the assets where they are needed (same parent folder as the md file), but to create an "assets" folder where they are stored

## Tech

- C# (.NET 9)
- OneOf (Nuget package not yet installed)
- vkhorikov/CSharpFunctionalExtensions (Nuget package not yet installed − DO PROPER ERROR HANDLING WITH RESULTS!)
- Use a functional programming style. Do NOT do inheritence, do NOT use classes if not needed, use records and static methods only if necessary, you may also use ref structs if needed, though it is probably not.

## Workflow hints

- Create a todo list of all the steps you need to do and follow them
- Every now and then, check with the specs to make sure you are still on the right track
- Create simple unit tests when needed
- Every now and then, do refactor the code
- At the end, generate a new version of this specs file with all the changes I ask you to make built in. Try to stay as true as possible to the original phrasing and structure of the specs. 
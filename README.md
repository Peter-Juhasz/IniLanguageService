# INI Language Service for Visual Studio
This extension adds support for editing .ini configuration files. It is built on the Managed Packaged Framework and written in C#.

Features:
 - Syntax highlighting and validation
 - Brace matching
 - Outlining
 - Highlight references (duplicates)
 - Automatic brace completion
 - Automatic formatting (while typing)
    - Section names (on ']')
    - Indentation of properties (on '=')
 - Diagnostics, code fixes and code refactorings
 - Quick info

![ini](https://cloud.githubusercontent.com/assets/9047283/8263461/1800a96a-16db-11e5-90ae-1526a173faf2.png)

Diagnostics:
 - Multiple declarations of a section
 - Redundant property declaration

Code Fixes:
 - Remove redundant property declaration
 - Merge declarations into first section

Code Refactorings:
 - Remove empty property declaration
 - Remove empty section

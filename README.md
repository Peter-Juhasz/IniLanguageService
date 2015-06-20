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

![ini](https://cloud.githubusercontent.com/assets/9047283/8266788/41fd8df2-1742-11e5-86e3-577baa9b1c44.png)

Diagnostics:
 - Multiple declarations of a section
 - Redundant property declaration

Code Fixes:
 - Remove redundant property declaration
 - Merge declarations into first section

Code Refactorings:
 - Remove empty property declaration
 - Remove empty section

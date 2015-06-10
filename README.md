# INI Language Service for Visual Studio
This extension adds support for editing .ini configuration files. It is built on the Managed Packaged Framework and written in C#.

Features:
 - Syntax highlighting and validation
 - Bracket matching
 - Outlining
 - Highlight references (duplicates)
 - Automatic formatting (while typing)
    - Section names (on ']')
    - Indentation of properties (on '=')
 - Diagnostics, code fixes and code refactorings
 - Quick info

Diagnostics:
 - Multiple declarations of a section
 - Redundant property declaration

Code Fixes:
 - Remove redundant property declaration
 - Merge declarations into first section

Code Refactorings:
 - Remove empty property declaration
 - Remove empty section

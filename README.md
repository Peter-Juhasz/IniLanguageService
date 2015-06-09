# INI Language Service for Visual Studio
This extension adds support for editing .ini configuration files. It is built on the Managed Packaged Framework and written in C#.

Features:
 - Syntax highlighting and validation
 - Bracket matching
 - Outlining
 - Automatic formatting (partial while typing)
 - Diagnostics, code fixes and code refactorings

Diagnostics:
 - Multiple declarations of a section
 - Redundant property declaration

Code Fixes:
 - Remove redundant property declaration
 - Merge declarations into first section

Code Refactorings:
 - Remove empty property declaration

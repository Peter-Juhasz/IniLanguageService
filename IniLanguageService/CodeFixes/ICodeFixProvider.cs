using IniLanguageService.CodeRefactorings;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace IniLanguageService.CodeFixes
{
    public interface ICodeFixProvider
    {
        IEnumerable<string> FixableDiagnosticIds { get; }

        IEnumerable<CodeAction> GetFixes(SnapshotSpan span);
    }
}

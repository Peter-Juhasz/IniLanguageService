using IniLanguageService.CodeRefactorings;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace IniLanguageService.CodeFixes
{
    public interface ICodeFixProvider
    {
        IEnumerable<string> FixableDiagnosticIds { get; }

        IEnumerable<CodeAction> GetFixes(SnapshotSpan span);
    }

    public static class CodeFixProviderExtensions
    {
        public static bool CanFix(this ICodeFixProvider provider, string diagnosticId)
        {
            return provider.FixableDiagnosticIds.Contains(diagnosticId);
        }
    }
}

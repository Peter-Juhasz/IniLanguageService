using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace IniLanguageService.CodeRefactorings
{
    public interface ICodeRefactoringProvider
    {
        IEnumerable<CodeAction> GetRefactorings(SnapshotSpan span);
    }
}

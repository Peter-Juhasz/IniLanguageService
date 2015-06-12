using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections.Generic;

namespace IniLanguageService.Diagnostics
{
    public interface ISyntaxNodeAnalyzer<TSyntaxNode> : IDiagnosticAnalyzer where TSyntaxNode : SyntaxNode
    {
        IEnumerable<ITagSpan<IErrorTag>> Analyze(TSyntaxNode node);
    }
}

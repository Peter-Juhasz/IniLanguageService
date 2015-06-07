using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace IniLanguageService.Syntax
{
    public abstract class SyntaxNode
    {
        public abstract SnapshotSpan Span { get; }

        public abstract IEnumerable<SnapshotToken> GetTokens();
    }
}

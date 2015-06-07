using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IniLanguageService.Syntax
{
    public abstract class SyntaxNode
    {
        public abstract SnapshotSpan Span { get; }

        public abstract IEnumerable<SnapshotToken> GetTokens();
    }
}

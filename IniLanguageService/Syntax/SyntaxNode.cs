using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace IniLanguageService.Syntax
{
    public abstract class SyntaxNode
    {
        public abstract SnapshotSpan Span { get; }

        public abstract SnapshotSpan FullSpan { get; }

        public abstract SyntaxNode Parent { get; }

        public abstract IEnumerable<SyntaxNode> Descendants();

        public abstract IEnumerable<SnapshotToken> GetTokens();
    }

    public static class SyntaxNodeExtensions
    {
        public static IEnumerable<SyntaxNode> DescendantsAndSelf(this SyntaxNode node)
        {
            yield return node;

            foreach (SyntaxNode n in node.Descendants())
                yield return n;
        }
    }
}

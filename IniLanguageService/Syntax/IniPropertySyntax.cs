using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace IniLanguageService.Syntax
{
    public class IniPropertySyntax : SyntaxNode
    {
        public IniPropertySyntax()
        {
            this.LeadingTrivia = new List<SnapshotToken>();
        }
        
        public IniSectionSyntax Section { get; set; }


        public SnapshotToken NameToken { get; set; }

        public SnapshotToken DelimiterToken { get; set; }

        public SnapshotToken ValueToken { get; set; }


        public List<SnapshotToken> LeadingTrivia { get; set; }

        public SnapshotToken TrailingTrivia { get; set; }


        public override SnapshotSpan Span
        {
            get
            {
                return new SnapshotSpan(this.NameToken.Span.Span.Start, this.ValueToken.Span.Span.End);
            }
        }


        public override IEnumerable<SnapshotToken> GetTokens()
        {
            foreach (SnapshotToken token in this.LeadingTrivia)
                yield return token;

            yield return this.NameToken;
            yield return this.DelimiterToken;
            yield return this.ValueToken;

            if (this.TrailingTrivia != null)
                yield return this.TrailingTrivia;
        }
    }
}

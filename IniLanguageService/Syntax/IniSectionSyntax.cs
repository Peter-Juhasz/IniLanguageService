using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace IniLanguageService.Syntax
{
    public class IniSectionSyntax : SyntaxNode
    {
        public IniSectionSyntax()
        {
            this.Properties = new List<IniPropertySyntax>();
            this.LeadingTrivia = new List<SnapshotToken>();
        }

        public IniDocumentSyntax Document { get; set; }

        public IList<IniPropertySyntax> Properties { get; set; }


        public SnapshotToken OpeningBracketToken { get; set; }

        public SnapshotToken NameToken { get; set; }

        public SnapshotToken ClosingBracketToken { get; set; }


        public IList<SnapshotToken> LeadingTrivia { get; set; }

        public SnapshotToken TrailingTrivia { get; set; }


        public override SnapshotSpan Span
        {
            get
            {
                return this.Properties.Count == 0
                    ? new SnapshotSpan(this.OpeningBracketToken.Span.Span.Start, this.ClosingBracketToken.Span.Span.End)
                    : new SnapshotSpan(this.OpeningBracketToken.Span.Span.Start, this.Properties.Last().TrailingTrivia.Span.Span.End)
                ;
            }
        }

        public override IEnumerable<SnapshotToken> GetTokens()
        {
            foreach (SnapshotToken token in this.LeadingTrivia)
                yield return token;

            yield return this.OpeningBracketToken;
            yield return this.NameToken;
            yield return this.ClosingBracketToken;

            if (this.TrailingTrivia != null)
                yield return this.TrailingTrivia;

            foreach (SnapshotToken token in this.Properties.SelectMany(p => p.GetTokens()))
                yield return token;
        }
    }
}

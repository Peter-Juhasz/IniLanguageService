using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IniLanguageService.Syntax
{
    public class IniDocumentSyntax : SyntaxNode
    {
        public IniDocumentSyntax()
        {
            this.Sections = new List<IniSectionSyntax>();
        }

        public ITextSnapshot Snapshot { get; set; }

        public IList<IniSectionSyntax> Sections { get; set; }


        public override SnapshotSpan Span
        {
            get
            {
                if (!this.Sections.Any())
                    return new SnapshotSpan(this.Snapshot, 0, 0);

                return new SnapshotSpan(
                    this.Sections.First().Span.Start,
                    this.Sections.Last().Span.End
                );
            }
        }


        public override IEnumerable<SnapshotToken> GetTokens()
        {
            return this.Sections.SelectMany(s => s.GetTokens());
        }
    }
}

using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace IniLanguageService
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType(ContentTypes.Ini)]
    internal class IniOutliningTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return new IniOutliningTagger() as ITagger<T>;
        }


        private sealed class IniOutliningTagger : ITagger<IOutliningRegionTag>
        {
            public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                ITextBuffer buffer = spans.First().Snapshot.TextBuffer;
                IniDocumentSyntax syntax = buffer.Properties.GetProperty<IniDocumentSyntax>("Syntax");
                
                return
                    from section in syntax.Sections
                    where section.Properties.Count > 0
                    where spans.Any(s => section.Span.IntersectsWith(s))
                    let last = section.Properties.Last()
                    let collapsibleSpan = new SnapshotSpan(
                        section.ClosingBracketToken.Span.Span.End,
                        (last.TrailingTrivia ?? last.ValueToken).Span.Span.End
                    )
                    select new TagSpan<IOutliningRegionTag>(
                        collapsibleSpan,
                        new OutliningRegionTag(
                            collapsedForm: "...",
                            collapsedHintForm: collapsibleSpan.GetText().Trim()
                        )
                    )
                ;
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        }
    }
}

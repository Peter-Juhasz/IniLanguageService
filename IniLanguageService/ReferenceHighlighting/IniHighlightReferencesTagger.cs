using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace IniLanguageService
{
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(ITextMarkerTag))]
    [ContentType(ContentTypes.Ini)]
    internal class IniHighlightReferencesTaggerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return new IniHighlightReferencesTagger(textView) as ITagger<T>;
        }


        private sealed class IniHighlightReferencesTagger : ITagger<ITextMarkerTag>
        {
            public IniHighlightReferencesTagger(ITextView view)
            {
                _view = view;

                _view.Caret.PositionChanged += OnCaretPositionChanged;
            }

            private readonly ITextView _view;

            private static readonly ITextMarkerTag Tag = new TextMarkerTag("MarkerFormatDefinition/HighlightedReference");


            private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
            {
                // TODO: optimize changed spans
                this.TagsChanged?.Invoke(this,
                    new SnapshotSpanEventArgs(new SnapshotSpan(e.TextView.TextBuffer.CurrentSnapshot, 0, e.TextView.TextBuffer.CurrentSnapshot.Length))
                );
            }
            
            public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                ITextBuffer buffer = spans.First().Snapshot.TextBuffer;
                IniDocumentSyntax syntax = buffer.Properties.GetProperty<IniDocumentSyntax>("Syntax");

                SnapshotPoint caret = _view.Caret.Position.BufferPosition;

                // find section
                IniSectionSyntax section = syntax.Sections
                    .FirstOrDefault(s => s.Span.Contains(caret));

                // show duplicate sections
                if (section != null)
                {
                    string sectionName = section.NameToken.Value;

                    // duplicate sections
                    if (!section.NameToken.IsMissing &&
                         section.NameToken.Span.Span.Contains(caret))
                    {
                        var others = section.Document.Sections
                            .Where(
                                s => s.NameToken.Value.Equals(sectionName, StringComparison.InvariantCultureIgnoreCase)
                            )
                            .ToList();

                        if (others.Count > 1)
                        {
                            return
                                from s in others
                                select new TagSpan<ITextMarkerTag>(s.NameToken.Span.Span, Tag)
                            ;
                        }
                    }

                    // duplicate properties
                    IniPropertySyntax property = section.Properties
                        .FirstOrDefault(p => p.NameToken.Span.Span.Contains(caret));

                    if (property != null)
                    {
                        string propertyName = property.NameToken.Value;

                        var others = (
                            from s in section.Document.Sections
                            where s.NameToken.Value.Equals(sectionName, StringComparison.InvariantCultureIgnoreCase)
                            from p in s.Properties
                            where p.NameToken.Value.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase)
                            select p
                        ).ToList();

                        if (others.Count > 1)
                        {
                            return
                                from s in others
                                select new TagSpan<ITextMarkerTag>(s.NameToken.Span.Span, Tag)
                            ;
                        }
                    }
                }

                return Enumerable.Empty<TagSpan<ITextMarkerTag>>();
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        }
    }
}

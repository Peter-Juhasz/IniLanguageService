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
    internal class IniBracketMatchingTaggerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return new IniBracketMatchingTagger(textView) as ITagger<T>;
        }


        private sealed class IniBracketMatchingTagger : ITagger<ITextMarkerTag>
        {
            public IniBracketMatchingTagger(ITextView view)
            {
                _view = view;

                _view.Caret.PositionChanged += OnCaretPositionChanged;
            }

            private readonly ITextView _view;

            private static readonly ITextMarkerTag Tag = new TextMarkerTag("bracehighlight");


            private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
            {
                ITextSnapshotLine oldLine = e.OldPosition.BufferPosition.GetContainingLine();
                ITextSnapshotLine newLine = e.NewPosition.BufferPosition.GetContainingLine();
                
                this.TagsChanged?.Invoke(this,
                    new SnapshotSpanEventArgs(new SnapshotSpan(newLine.Start, newLine.End))
                );

                if (newLine != oldLine)
                {
                    this.TagsChanged?.Invoke(this,
                        new SnapshotSpanEventArgs(new SnapshotSpan(oldLine.Start, oldLine.End))
                    );
                }
            }
            
            public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                ITextBuffer buffer = spans.First().Snapshot.TextBuffer;
                SyntaxTree syntax = buffer.GetSyntaxTree();
                IniDocumentSyntax root = syntax.Root as IniDocumentSyntax;

                SnapshotPoint caret = _view.Caret.Position.BufferPosition;

                IniSectionSyntax section = root.Sections
                    .Where(
                        s => !s.OpeningBracketToken.IsMissing
                          && !s.ClosingBracketToken.IsMissing
                    )
                    .FirstOrDefault(
                        s => s.OpeningBracketToken.Span.Span.Start == caret
                          || s.ClosingBracketToken.Span.Span.End == caret
                    );

                if (section != null)
                {
                    yield return new TagSpan<ITextMarkerTag>(section.OpeningBracketToken.Span.Span, Tag);
                    yield return new TagSpan<ITextMarkerTag>(section.ClosingBracketToken.Span.Span, Tag);
                }
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        }
    }
}

﻿using IniLanguageService.Syntax;
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
    [ContentType(IniContentTypeNames.Ini)]
    internal sealed class IniHighlightMultipleReferencesTaggerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return textView.Properties.GetOrCreateSingletonProperty(
                creator: () => new IniHighlightMultipleReferencesTagger(textView)
            ) as ITagger<T>;
        }


        private sealed class IniHighlightMultipleReferencesTagger : ITagger<ITextMarkerTag>
        {
            public IniHighlightMultipleReferencesTagger(ITextView view)
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
                SyntaxTree syntax = buffer.GetSyntaxTree();
                IniDocumentSyntax root = syntax.Root as IniDocumentSyntax;
                

                SnapshotPoint caret = _view.Caret.Position.BufferPosition;

                // find section
                IniSectionSyntax section = root.Sections
                    .FirstOrDefault(s => s.Span.ContainsOrEndsWith(caret));

                // show duplicate sections
                if (section != null)
                {
                    string sectionName = section.NameToken.Value;

                    // duplicate sections
                    if (!section.NameToken.IsMissing &&
                         section.NameToken.Span.Span.ContainsOrEndsWith(caret))
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
                        .FirstOrDefault(p => p.NameToken.Span.Span.ContainsOrEndsWith(caret));

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

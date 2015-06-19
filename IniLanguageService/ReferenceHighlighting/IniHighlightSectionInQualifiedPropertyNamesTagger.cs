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
    [ContentType(IniContentTypeNames.Ini)]
    internal sealed class IniHighlightSectionInQualifiedPropertyNamesTaggerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return textView.Properties.GetOrCreateSingletonProperty(
                creator: () => new IniHighlightSectionInQualifiedPropertyNamesTagger(textView)
            ) as ITagger<T>;
        }


        private sealed class IniHighlightSectionInQualifiedPropertyNamesTagger : ITagger<ITextMarkerTag>
        {
            public IniHighlightSectionInQualifiedPropertyNamesTagger(ITextView view)
            {
                _view = view;

                _view.Caret.PositionChanged += OnCaretPositionChanged;
            }

            private readonly ITextView _view;

            private static readonly IReadOnlyCollection<char> Delimiters = new [] { '.', '\\', ':', '/' };

            private static readonly ITextMarkerTag DefinitionTag = new TextMarkerTag("MarkerFormatDefinition/HighlightedDefinition");
            private static readonly ITextMarkerTag ReferenceTag = new TextMarkerTag("MarkerFormatDefinition/HighlightedReference");


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
                    .Where(s => !s.NameToken.IsMissing)
                    .FirstOrDefault(s => s.Span.ContainsOrEndsWith(caret));

                // find results
                if (section != null)
                {
                    string sectionName = section.NameToken.Value;

                    IReadOnlyCollection<IniSectionSyntax> matchingSections = root
                        .Sections
                        .Where(s => s.NameToken.Value.Equals(sectionName, StringComparison.InvariantCultureIgnoreCase))
                        .ToList();

                    IReadOnlyCollection<IniPropertySyntax> matchingProperties = matchingSections
                        .SelectMany(s => s.Properties)
                        .Where(p => p.NameToken.Value.StartsWith(sectionName, StringComparison.InvariantCultureIgnoreCase))
                        .Where(p => p.NameToken.Value.Length > sectionName.Length)
                        .Where(p => Delimiters.Contains(p.NameToken.Value[sectionName.Length]))
                        .ToList();

                    if (matchingProperties.Any())
                    {
                        if (section.NameToken.Span.Span.Contains(caret) ||
                            matchingProperties.Any(p => new SnapshotSpan(p.NameToken.Span.Span.Start, sectionName.Length).ContainsOrEndsWith(caret)))
                        {
                            foreach (var s in matchingSections)
                                yield return new TagSpan<ITextMarkerTag>(s.NameToken.Span.Span, DefinitionTag);

                            foreach (var property in matchingProperties)
                                yield return new TagSpan<ITextMarkerTag>(
                                    new SnapshotSpan(property.NameToken.Span.Span.Start, sectionName.Length),
                                    ReferenceTag
                                );
                        }
                    }
                }
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        }
    }
}

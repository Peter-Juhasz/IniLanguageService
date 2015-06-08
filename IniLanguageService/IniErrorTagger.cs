using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace IniLanguageService
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IErrorTag))]
    [ContentType(ContentTypes.Ini)]
    public class IniErrorTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return new IniErrorTagger() as ITagger<T>;
        }

        private sealed class IniErrorTagger : ITagger<IErrorTag>
        {
            public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                ITextBuffer buffer = spans.First().Snapshot.TextBuffer;
                IniDocumentSyntax syntax = buffer.Properties.GetProperty<IniDocumentSyntax>("Syntax");
                
                return
                    from section in syntax.Sections
                    where spans.Any(s => section.Span.IntersectsWith(s))
                    from diagnostic in Analyze(section)
                    where spans.Any(s => diagnostic.Span.IntersectsWith(s))
                    select diagnostic
                ;
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

            private static IEnumerable<TagSpan<IErrorTag>> Analyze(IniSectionSyntax section)
            {
                // section name is missing
                if (section.NameToken.IsMissing)
                {
                    yield return new TagSpan<IErrorTag>(
                        section.NameToken.Span.Span,
                        new ErrorTag(PredefinedErrorTypeNames.SyntaxError, "Section name expected")
                    );
                }

                // closing bracket is missing
                else if (section.ClosingBracketToken.IsMissing)
                {
                    yield return new TagSpan<IErrorTag>(
                        section.ClosingBracketToken.Span.Span,
                        new ErrorTag(PredefinedErrorTypeNames.SyntaxError, "']' expected")
                    );
                }

                // get child diagnostics
                foreach (var diagnostic in section.Properties.SelectMany(Analyze))
                    yield return diagnostic;
                

                // check for duplicate sections
                string name = section.NameToken.Span.Span.GetText();

                var other = section.Document.Sections
                    .FirstOrDefault(
                        s => s.NameToken.Span.Span.GetText().Equals(name, StringComparison.InvariantCultureIgnoreCase)
                    );

                if (other != null && other != section)
                {
                    yield return new TagSpan<IErrorTag>(
                        section.NameToken.Span.Span,
                        new ErrorTag(PredefinedErrorTypeNames.Warning, $"Multiple declarations of section '{name}'")
                    );
                }
            }

            private static IEnumerable<TagSpan<IErrorTag>> Analyze(IniPropertySyntax property)
            {
                // delimiter missing
                if (property.DelimiterToken.IsMissing)
                {
                    yield return new TagSpan<IErrorTag>(
                        property.DelimiterToken.Span.Span,
                        new ErrorTag(PredefinedErrorTypeNames.SyntaxError, "'=' expected")
                    );
                }
                else
                {
                    // value is missing
                    if (property.PropertyValueToken.IsMissing)
                    {
                        yield return new TagSpan<IErrorTag>(
                            property.PropertyValueToken.Span.Span,
                            new ErrorTag(PredefinedErrorTypeNames.SyntaxError, "Property value expected")
                        );
                    }


                    // check for duplicate properties
                    string sectionName = property.Section.NameToken.Span.Span.GetText();
                    string name = property.PropertyNameToken.Span.Span.GetText();

                    var other = property.Section.Document.Sections
                        .Where(s => s.NameToken.Span.Span.GetText().Equals(sectionName, StringComparison.InvariantCultureIgnoreCase))
                        .SelectMany(s => s.Properties)
                        .FirstOrDefault(
                            p => p.PropertyNameToken.Span.Span.GetText().Equals(name, StringComparison.InvariantCultureIgnoreCase)
                        );

                    if (other != null && other != property)
                    {
                        yield return new TagSpan<IErrorTag>(
                            property.PropertyNameToken.Span.Span,
                            new ErrorTag(PredefinedErrorTypeNames.Warning, $"Multiple declarations of property '{name}'")
                        );
                    }
                }
            }
        }
    }
}

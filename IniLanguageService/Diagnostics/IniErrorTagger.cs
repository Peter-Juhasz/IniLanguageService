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
    [ContentType(IniContentTypeNames.Ini)]
    internal sealed class IniErrorTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(
                creator: () => new IniErrorTagger()
            ) as ITagger<T>;
        }


        private sealed class IniErrorTagger : ITagger<IErrorTag>
        {
            public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                ITextBuffer buffer = spans.First().Snapshot.TextBuffer;
                SyntaxTree syntax = buffer.GetSyntaxTree();
                IniDocumentSyntax root = syntax.Root as IniDocumentSyntax;

                return
                    from section in root.Sections
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
                        new DiagnosticErrorTag(PredefinedErrorTypeNames.SyntaxError, "SectionNameClosingBracketMissing", "']' expected")
                    );
                }

                // get child diagnostics
                foreach (var diagnostic in section.Properties.SelectMany(Analyze))
                    yield return diagnostic;
                

                // check for duplicate sections
                string name = section.NameToken.Value;

                var other = section.Document.Sections
                    .FirstOrDefault(
                        s => s.NameToken.Value.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                    );

                if (other != section)
                {
                    yield return new TagSpan<IErrorTag>(
                        section.NameToken.Span.Span,
                        new DiagnosticErrorTag(PredefinedErrorTypeNames.Warning, "MultipleDeclarationsOfSection", $"Multiple declarations of section '{name}'")
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
                        new DiagnosticErrorTag(PredefinedErrorTypeNames.SyntaxError, "PropertyNameValueDelimiterExpected", "'=' expected")
                    );
                }
                else
                {
                    // check for duplicate properties
                    string sectionName = property.Section.NameToken.Value;
                    string name = property.NameToken.Value;

                    var propertiesWithSameName = (
                        from s in property.Section.Document.Sections
                        where s.NameToken.Value.Equals(sectionName, StringComparison.InvariantCultureIgnoreCase)
                        from p in s.Properties
                        where p.NameToken.Value.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                        select p
                    ).ToList();

                    // check for redundant declarations (name and value matches)
                    string value = property.ValueToken.Value;

                    var other = propertiesWithSameName
                        .FirstOrDefault(p => p.ValueToken.Value == value);

                    if (other != property)
                    {
                        yield return new TagSpan<IErrorTag>(
                            property.Span,
                            new DiagnosticErrorTag(PredefinedErrorTypeNames.Warning, "RedundantPropertyDeclaration", $"Redundant declaration of property '{name}'")
                        );
                    }

                    // report only name equality
                    else
                    {
                        other = propertiesWithSameName.FirstOrDefault();

                        if (other != property)
                        {
                            yield return new TagSpan<IErrorTag>(
                                property.NameToken.Span.Span,
                                new ErrorTag(PredefinedErrorTypeNames.Warning, $"Multiple declarations of property '{name}'")
                            );
                        }
                    }
                }
            }
        }
    }
}

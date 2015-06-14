using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace IniLanguageService
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("INI Quick Info Provider")]
    [ContentType(IniContentTypeNames.Ini)]
    internal class IniQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new IniQuickInfoSource(textBuffer);
        }


        private sealed class IniQuickInfoSource : IQuickInfoSource
        {
            public IniQuickInfoSource(ITextBuffer buffer)
            {
                _buffer = buffer;
            }

            private readonly ITextBuffer _buffer;


            public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
            {
                ITextSnapshot snapshot = _buffer.CurrentSnapshot;
                ITrackingPoint triggerPoint = session.GetTriggerPoint(_buffer);
                SnapshotPoint point = triggerPoint.GetPoint(snapshot);

                SyntaxTree syntax = snapshot.GetSyntaxTree();
                IniDocumentSyntax root = syntax.Root as IniDocumentSyntax;

                applicableToSpan = null;

                // find section
                IniSectionSyntax section = root.Sections
                    .FirstOrDefault(s => s.Span.Span.Contains(point));

                if (section != null)
                {
                    string sectionName = section.NameToken.Value;

                    // provide info about section
                    if (!section.NameToken.IsMissing &&
                        section.NameToken.Span.Span.Contains(point))
                    {
                        // construct content
                        string content = $"(section) {sectionName}";

                        var trivia = section.LeadingTrivia.SwitchToIfEmpty(section.TrailingTrivia);
                        if (section.LeadingTrivia.Any())
                        {
                            content += Environment.NewLine +
                                String.Join(Environment.NewLine,
                                    trivia
                                        .Select(t => t.Value)
                                        .Select(v => v.Substring(1).Trim()) // TODO: move to value
                                )
                            ;
                        }

                        // add to session
                        quickInfoContent.Add(content);
                        applicableToSpan = snapshot.CreateTrackingSpan(section.NameToken.Span.Span, SpanTrackingMode.EdgeInclusive);
                        return;
                    }

                    // provide info about property
                    IniPropertySyntax property = section.Properties
                        .FirstOrDefault(p => p.NameToken.Span.Span.Contains(point));

                    if (property != null)
                    {
                        string propertyName = property.NameToken.Value;

                        // construct content
                        string content = $"(property) {propertyName} (in {sectionName})";

                        var trivia = property.LeadingTrivia.SwitchToIfEmpty(property.TrailingTrivia);
                        if (trivia.Any())
                        {
                            content += Environment.NewLine +
                                String.Join(Environment.NewLine,
                                    trivia
                                        .Select(t => t.Value)
                                        .Select(v => v.Substring(1).Trim()) // TODO: move to value
                                )
                            ;
                        }

                        // add to session
                        quickInfoContent.Add(content);
                        applicableToSpan = snapshot.CreateTrackingSpan(property.NameToken.Span.Span, SpanTrackingMode.EdgeInclusive);
                        return;
                    }
                }
            }

            void IDisposable.Dispose()
            { }
        }
    }
}

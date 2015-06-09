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
    [ContentType(ContentTypes.Ini)]
    internal class IniQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new IniQuickInfoSource(textBuffer);
        }


        private sealed class IniQuickInfoSource : IQuickInfoSource
        {
            public IniQuickInfoSource(ITextBuffer textBuffer)
            {
                _buffer = textBuffer;
            }

            private ITextBuffer _buffer;


            public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
            {
                ITextSnapshot snapshot = _buffer.CurrentSnapshot;
                ITrackingPoint triggerPoint = session.GetTriggerPoint(_buffer);
                SnapshotPoint point = triggerPoint.GetPoint(snapshot);

                IniDocumentSyntax syntax = _buffer.Properties.GetProperty<IniDocumentSyntax>("Syntax");

                applicableToSpan = null;

                // find section
                IniSectionSyntax section = syntax.Sections
                    .FirstOrDefault(s => s.Span.Span.Contains(point));

                if (section != null)
                {
                    string sectionName = section.NameToken.Value;

                    // provide info about section
                    if (!section.NameToken.IsMissing &&
                        section.NameToken.Span.Span.Contains(point))
                    {
                        quickInfoContent.Add($"(section) {sectionName}");
                        applicableToSpan = snapshot.CreateTrackingSpan(section.NameToken.Span.Span, SpanTrackingMode.EdgeInclusive);
                        return;
                    }

                    // provide info about property
                    IniPropertySyntax property = section.Properties
                        .FirstOrDefault(p => p.NameToken.Span.Span.Contains(point));

                    if (property != null)
                    {
                        string propertyName = property.NameToken.Value;
                        quickInfoContent.Add($"(property) {propertyName} (in {sectionName})");
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

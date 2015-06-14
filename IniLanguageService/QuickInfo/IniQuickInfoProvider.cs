using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace IniLanguageService.QuickInfo
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("INI Quick Info Provider")]
    [ContentType(IniContentTypeNames.Ini)]
    internal sealed class IniQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
#pragma warning disable 649

        [Import]
        private IGlyphService glyphService;

#pragma warning restore 649


        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(
                creator: () => new IniQuickInfoSource(textBuffer, glyphService)
            );
        }


        private sealed class IniQuickInfoSource : IQuickInfoSource
        {
            public IniQuickInfoSource(ITextBuffer buffer, IGlyphService glyphService)
            {
                _buffer = buffer;
                _glyphService = glyphService;
            }

            private readonly ITextBuffer _buffer;
            private readonly IGlyphService _glyphService;

            private static readonly DataTemplate Template;
        

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
                        // get glyph
                        var glyph = _glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic);

                        // construct content
                        var content = new QuickInfoContent
                        {
                            Glyph = glyph,
                            Signature = sectionName
                        };
                        
                        var trivia = section.LeadingTrivia.SwitchToIfEmpty(section.TrailingTrivia);
                        if (section.LeadingTrivia.Any())
                        {
                            content.Documentation = String.Join(Environment.NewLine,
                                trivia
                                    .Select(t => t.Value)
                                    .Select(v => v.Substring(1).Trim()) // TODO: move to value
                            );
                        }
                        
                        // add to session
                        quickInfoContent.Add(
                            new ContentPresenter
                            {
                                Content = content,
                                ContentTemplate = Template,
                            }
                        );
                        applicableToSpan = snapshot.CreateTrackingSpan(section.NameToken.Span.Span, SpanTrackingMode.EdgeInclusive);
                        return;
                    }

                    // provide info about property
                    IniPropertySyntax property = section.Properties
                        .FirstOrDefault(p => p.NameToken.Span.Span.Contains(point));

                    if (property != null)
                    {
                        string propertyName = property.NameToken.Value;

                        // get glyph
                        var glyph = _glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic);

                        // construct content
                        var content = new QuickInfoContent
                        {
                            Glyph = glyph,
                            Signature = propertyName
                        };
                        
                        var trivia = property.LeadingTrivia.SwitchToIfEmpty(property.TrailingTrivia);
                        if (trivia.Any())
                        {
                            content.Documentation = String.Join(Environment.NewLine,
                                trivia
                                    .Select(t => t.Value)
                                    .Select(v => v.Substring(1).Trim()) // TODO: move to value
                            );
                        }

                        // add to session
                        quickInfoContent.Add(
                            new ContentPresenter
                            {
                                Content = content,
                                ContentTemplate = Template,
                            }
                        );
                        applicableToSpan = snapshot.CreateTrackingSpan(property.NameToken.Span.Span, SpanTrackingMode.EdgeInclusive);
                        return;
                    }
                }
            }

            void IDisposable.Dispose()
            { }


            static IniQuickInfoSource()
            {
                var resources = new ResourceDictionary { Source = new Uri("pack://application:,,,/IniLanguageService;component/Themes/Generic.xaml", UriKind.RelativeOrAbsolute) };

                Template = resources.Values.OfType<DataTemplate>().First();
            }
        }
    }
}

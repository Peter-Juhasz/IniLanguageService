using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.TextFormatting;

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

        [Import]
        private IClassificationTypeRegistryService classificationRegistry;

        [Import]
        private IClassificationFormatMapService classificationFormatMapService;

#pragma warning restore 649


        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(
                creator: () => new IniQuickInfoSource(
                    textBuffer,
                    glyphService,
                    classificationFormatMapService, 
                    classificationRegistry
                )
            );
        }


        private sealed class IniQuickInfoSource : IQuickInfoSource
        {
            public IniQuickInfoSource(
                ITextBuffer buffer,
                IGlyphService glyphService,
                IClassificationFormatMapService classificationFormatMapService,
                IClassificationTypeRegistryService classificationRegistry
            )
            {
                
                _buffer = buffer;
                _glyphService = glyphService;
                _classificationFormatMapService = classificationFormatMapService;
                _classificationRegistry = classificationRegistry;
            }

            private readonly ITextBuffer _buffer;
            private readonly IGlyphService _glyphService;
            private readonly IClassificationFormatMapService _classificationFormatMapService;
            private readonly IClassificationTypeRegistryService _classificationRegistry;

            private static readonly DataTemplate Template;
        

            public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
            {
                ITextSnapshot snapshot = _buffer.CurrentSnapshot;
                ITrackingPoint triggerPoint = session.GetTriggerPoint(_buffer);
                SnapshotPoint point = triggerPoint.GetPoint(snapshot);

                SyntaxTree syntax = snapshot.GetSyntaxTree();
                IniDocumentSyntax root = syntax.Root as IniDocumentSyntax;

                IClassificationFormatMap formatMap = _classificationFormatMapService.GetClassificationFormatMap(session.TextView);

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
                        // get glyph and rich formatting
                        var glyph = _glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic);
                        var classificationType = _classificationRegistry.GetClassificationType("INI/SectionName");
                        var format = formatMap.GetTextProperties(classificationType);

                        // construct content
                        var content = new QuickInfoContent
                        {
                            Glyph = glyph,
                            Signature = new Run(sectionName) { Foreground = format.ForegroundBrush },
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
                        var classificationType = _classificationRegistry.GetClassificationType("INI/PropertyName");
                        var format = formatMap.GetTextProperties(classificationType);

                        // construct content
                        var content = new QuickInfoContent
                        {
                            Glyph = glyph,
                            Signature = new Run(propertyName) { Foreground = format.ForegroundBrush },
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

using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace IniLanguageService
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("Ini Suggested Actions")]
    [ContentType(ContentTypes.Ini)]
    public class IniSuggestedActionsProvider : ISuggestedActionsSourceProvider
    {
        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            return new IniSuggestedActions(textView);
        }


        private sealed class IniSuggestedActions : ISuggestedActionsSource
        {
            public IniSuggestedActions(ITextView view)
            {
                _view = view;
            }

            private readonly ITextView _view;

            public event EventHandler<EventArgs> SuggestedActionsChanged;

            public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
            {
                ITextBuffer buffer = range.Snapshot.TextBuffer;
                IniDocumentSyntax syntax = buffer.Properties.GetProperty<IniDocumentSyntax>("Syntax");
                
                yield return new SuggestedActionSet(
                    (
                        from section in syntax.Sections
                        where !section.NameToken.IsMissing
                        where section.NameToken.Span.Span.IntersectsWith(range)
                        let name = section.NameToken.Span.Span.GetText()
                        let firstSection = syntax.Sections.First(
                            s => s.NameToken.Span.Span.GetText().Equals(name, StringComparison.InvariantCultureIgnoreCase)
                        )
                        where section != firstSection
                        select new MovePropertiesToFirstSection(firstSection, section)
                    ).ToArray()
                );
            }

            public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
            {
                ITextBuffer buffer = range.Snapshot.TextBuffer;
                IniDocumentSyntax syntax = buffer.Properties.GetProperty<IniDocumentSyntax>("Syntax");

                return Task.FromResult(
                    (
                        from section in syntax.Sections
                        where !section.NameToken.IsMissing
                        where section.NameToken.Span.Span.IntersectsWith(range)
                        let name = section.NameToken.Span.Span.GetText()
                        let firstSection = syntax.Sections.First(
                            s => s.NameToken.Span.Span.GetText().Equals(name, StringComparison.InvariantCultureIgnoreCase)
                        )
                        where section != firstSection
                        select section
                    ).Any()
                );
            }


            public bool TryGetTelemetryId(out Guid telemetryId)
            {
                telemetryId = Guid.Empty;
                return false;
            }

            void IDisposable.Dispose()
            { }


            private sealed class MovePropertiesToFirstSection : ISuggestedAction
            {
                public MovePropertiesToFirstSection(IniSectionSyntax baseSection, IniSectionSyntax otherSection)
                {
                    _base = baseSection;
                    _other = otherSection;
                }

                private readonly IniSectionSyntax _base;
                private readonly IniSectionSyntax _other;

                public IEnumerable<SuggestedActionSet> ActionSets
                {
                    get
                    {
                        return Enumerable.Empty<SuggestedActionSet>();
                    }
                }

                public string DisplayText
                {
                    get
                    {
                        return $"Merge declarations into the first '{_base.NameToken.Span.Span.GetText()}' section";
                    }
                }

                public string IconAutomationText
                {
                    get
                    {
                        return null;
                    }
                }

                public ImageSource IconSource
                {
                    get
                    {
                        return null;
                    }
                }

                public string InputGestureText
                {
                    get
                    {
                        return null;
                    }
                }
                
                public object GetPreview(CancellationToken cancellationToken)
                {
                    return null;
                }

                public void Invoke(CancellationToken cancellationToken)
                {
                    ITextBuffer buffer = _base.Document.Snapshot.TextBuffer;

                    using (ITextEdit edit = buffer.CreateEdit())
                    {
                        edit.Insert(
                            _base.Span.End,
                            new SnapshotSpan(
                                _other.ClosingBracketToken.Span.Span.End,
                                _other.Span.End
                            ).GetText()
                        );
                        edit.Delete(_other.Span);
                        
                        edit.Apply();
                    }
                }


                public bool TryGetTelemetryId(out Guid telemetryId)
                {
                    telemetryId = Guid.Empty;
                    return false;
                }
                
                void IDisposable.Dispose()
                { }
            }
        }
    }
}

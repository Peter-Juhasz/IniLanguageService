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
    //[Export(typeof(ISuggestedActionsSourceProvider))]
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

                IniSectionSyntax section = syntax.Sections
                    .Where(s => !s.NameToken.IsMissing)
                    .First(s => s.NameToken.Span.Span.IntersectsWith(range));

                yield return new SuggestedActionSet(
                    new []
                    {
                        new MovePropertiesToFirstSection(section, section)
                    },
                    applicableToSpan: section.NameToken.Span.Span
                );
            }

            public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
            {
                ITextBuffer buffer = range.Snapshot.TextBuffer;
                IniDocumentSyntax syntax = buffer.Properties.GetProperty<IniDocumentSyntax>("Syntax");

                return Task.FromResult(
                    syntax.Sections.Any(
                        s => !s.NameToken.IsMissing && s.NameToken.Span.Span.IntersectsWith(range)
                    )
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

                }

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
                        return "Merge properties into the first section";
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
                    throw new NotImplementedException();
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

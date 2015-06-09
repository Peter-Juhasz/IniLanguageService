using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Media;

namespace IniLanguageService.CodeFixes
{
    internal sealed class MergeDeclarationsIntoFirstSection : ISuggestedAction
    {
        public MergeDeclarationsIntoFirstSection(SnapshotSpan diagnosticSpan)
        {
            _span = diagnosticSpan;

            ITextBuffer buffer = _span.Snapshot.TextBuffer;
            IniDocumentSyntax syntax = buffer.Properties.GetProperty<IniDocumentSyntax>("Syntax");

            _current = syntax.Sections.First(s => s.NameToken.Span.Span == diagnosticSpan);
            string sectionName = _current.NameToken.Value;
            _base = syntax.Sections.First(s => s.NameToken.Value.Equals(sectionName, StringComparison.InvariantCultureIgnoreCase));
        }
        
        private readonly SnapshotSpan _span;

        private readonly IniSectionSyntax _base;
        private readonly IniSectionSyntax _current;

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
                        _current.ClosingBracketToken.Span.Span.End,
                        _current.Span.End
                    ).GetText()
                );
                edit.Delete(_current.Span);

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

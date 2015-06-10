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
    internal sealed class RemoveEmptySection : ISuggestedAction
    {
        public RemoveEmptySection(SnapshotSpan diagnosticSpan)
        {
            _span = diagnosticSpan;

            ITextBuffer buffer = _span.Snapshot.TextBuffer;
            IniDocumentSyntax syntax = buffer.Properties.GetProperty<IniDocumentSyntax>("Syntax");

            _section = syntax.Sections
                .First(s => s.Span == diagnosticSpan);
        }
        
        private readonly SnapshotSpan _span;

        private readonly IniSectionSyntax _section;

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
                return $"Remove empty section";
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
            ITextBuffer buffer = _section.Document.Snapshot.TextBuffer;

            using (ITextEdit edit = buffer.CreateEdit())
            {
                edit.Delete(_section.Span);

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

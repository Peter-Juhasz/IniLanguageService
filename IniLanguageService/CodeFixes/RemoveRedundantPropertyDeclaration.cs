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
    internal sealed class RemoveRedundantPropertyDeclaration : ISuggestedAction
    {
        public RemoveRedundantPropertyDeclaration(SnapshotSpan diagnosticSpan)
        {
            _span = diagnosticSpan;

            ITextBuffer buffer = _span.Snapshot.TextBuffer;
            IniDocumentSyntax syntax = buffer.Properties.GetProperty<IniDocumentSyntax>("Syntax");

            _property = syntax.Sections
                .SelectMany(s => s.Properties)
                .First(s => s.Span == diagnosticSpan);
        }
        
        private readonly SnapshotSpan _span;

        private readonly IniPropertySyntax _property;

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
                return $"Remove redundant declaration";
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
            ITextBuffer buffer = _property.Section.Document.Snapshot.TextBuffer;

            using (ITextEdit edit = buffer.CreateEdit())
            {
                ITextSnapshotLine line = _property.Span.Start.GetContainingLine();
                edit.Delete(new SnapshotSpan(line.Start, line.EndIncludingLineBreak));

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

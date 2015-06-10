using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Media;

namespace IniLanguageService.CodeRefactorings
{
    public class CodeAction
    {
        public CodeAction(string title, Func<ITextSnapshot, ITextSnapshot> transform)
        {
            this._title = title;
            this._transform = transform;
        }
        public CodeAction(string title, Func<ITextSnapshot, ITextEdit> edit)
            : this(title, s => ApplyEdit(edit(s)))
        { }

        private readonly Func<ITextSnapshot, ITextSnapshot> _transform;
        private readonly string _title;


        public string Title
        {
            get
            {
                return _title;
            }
        }

        public virtual ITextSnapshot Apply(ITextSnapshot snapshot)
        {
            return _transform(snapshot);
        }


        private static ITextSnapshot ApplyEdit(ITextEdit edit)
        {
            using (edit)
                return edit.Apply();
        }
    }

    public static class CodeActionExtensions
    {
        public static ISuggestedAction ToSuggestedAction(this CodeAction action, ITextSnapshot snapshot)
        {
            return new CodeActionSuggestedAction(action, snapshot);
        }

        private sealed class CodeActionSuggestedAction : ISuggestedAction
        {
            public CodeActionSuggestedAction(CodeAction action, ITextSnapshot snapshot)
            {
                this._action = action;
                this._snapshot = snapshot;
            }

            private readonly CodeAction _action;
            private readonly ITextSnapshot _snapshot;

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
                    return _action.Title;
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
                _action.Apply(_snapshot);
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

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
        public CodeAction(string title, Func<ITextSnapshot> transform)
        {
            this._title = title;
            this._transform = transform;
        }
        public CodeAction(string title, Func<ITextEdit> edit)
            : this(title, () => ApplyEdit(edit()))
        { }

        private readonly Func<ITextSnapshot> _transform;
        private readonly string _title;


        public string Title
        {
            get
            {
                return _title;
            }
        }

        public virtual ITextSnapshot Apply()
        {
            return _transform();
        }


        private static ITextSnapshot ApplyEdit(ITextEdit edit)
        {
            using (edit)
                return edit.Apply();
        }
    }

    public static class CodeActionExtensions
    {
        public static ISuggestedAction ToSuggestedAction(this CodeAction action)
        {
            return new CodeActionSuggestedAction(action);
        }

        private sealed class CodeActionSuggestedAction : ISuggestedAction
        {
            public CodeActionSuggestedAction(CodeAction action)
            {
                this._action = action;
            }

            private readonly CodeAction _action;

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
                _action.Apply();
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

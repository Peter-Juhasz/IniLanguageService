using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

            public string InputGestureText
            {
                get
                {
                    return null;
                }
            }

            public ImageMoniker IconMoniker
            {
                get
                {
                    return KnownMonikers.None;
                }
            }

            public void Invoke(CancellationToken cancellationToken)
            {
                _action.Apply();
            }


            public bool HasActionSets
            {
                get
                {
                    return false;
                }
            }

            public async Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
            {
                return Enumerable.Empty<SuggestedActionSet>();
            }

            public bool HasPreview
            {
                get
                {
                    return false;
                }
            }

            public async Task<object> GetPreviewAsync(CancellationToken cancellationToken)
            {
                return null;
            }


            bool ITelemetryIdProvider<Guid>.TryGetTelemetryId(out Guid telemetryId)
            {
                telemetryId = Guid.Empty;
                return false;
            }

            void IDisposable.Dispose()
            { }
        }
    }
}

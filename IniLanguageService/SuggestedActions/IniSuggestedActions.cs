using IniLanguageService.CodeFixes;
using IniLanguageService.CodeRefactorings;
using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IniLanguageService
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("Ini Suggested Actions")]
    [ContentType(IniContentTypeNames.Ini)]
    internal sealed class IniSuggestedActionsProvider : ISuggestedActionsSourceProvider
    {
#pragma warning disable 649

        [Import]
        private IBufferTagAggregatorFactoryService aggregatorFactoryService;

        [ImportMany]
        private IEnumerable<ICodeFixProvider> codeFixProviders;

        [ImportMany]
        private IEnumerable<ICodeRefactoringProvider> refactoringProviders;

#pragma warning restore 649


        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(
                creator: () => new IniSuggestedActions(
                    textView, textBuffer,
                    aggregatorFactoryService.CreateTagAggregator<IErrorTag>(textBuffer),
                    codeFixProviders,
                    refactoringProviders
                )
            );
        }


        private sealed class IniSuggestedActions : ISuggestedActionsSource
        {
            public IniSuggestedActions(
                ITextView view, ITextBuffer buffer,
                ITagAggregator<IErrorTag> aggregator,
                IEnumerable<ICodeFixProvider> codeFixProviders,
                IEnumerable<ICodeRefactoringProvider> refactoringProviders
            )
            {
                _view = view;
                _buffer = buffer;
                _aggregator = aggregator;
                _codeFixProviders = codeFixProviders;
                _refactoringProviders = refactoringProviders;
            }

            private readonly ITextView _view;
            private readonly ITextBuffer _buffer;
            private readonly ITagAggregator<IErrorTag> _aggregator;
            private readonly IEnumerable<ICodeFixProvider> _codeFixProviders;
            private readonly IEnumerable<ICodeRefactoringProvider> _refactoringProviders;
                        
            public event EventHandler<EventArgs> SuggestedActionsChanged;

            public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
            {
                return
                    (
                        // code fixes
                        from tagSpan in _aggregator.GetTags(range)
                        where tagSpan.Tag is DiagnosticErrorTag
                        let diagnostic = tagSpan.Tag as DiagnosticErrorTag

                        from provider in _codeFixProviders
                        where provider.CanFix(diagnostic.Id)
                        let span = tagSpan.Span.GetSpans(_buffer).First()

                        from fix in provider.GetFixes(span)

                        group fix by provider into set
                        where set.Any()
                        select set as IEnumerable<CodeAction>
                    ).Union(
                        // code refactorings
                        from provider in _refactoringProviders
                        from refactoring in provider.GetRefactorings(range)

                        group refactoring by provider into set
                        where set.Any()
                        select set as IEnumerable<CodeAction>
                    )
                    .Select(s => s.Select(ca => ca.ToSuggestedAction()))
                    .Select(s => new SuggestedActionSet(s))
                ;
            }

            public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
            {
                ITextBuffer buffer = range.Snapshot.TextBuffer;

                return Task.FromResult(
                    _aggregator.GetTags(range)
                        .Select(s => s.Tag)
                        .OfType<DiagnosticErrorTag>()
                        .Any(t => IsFixable(t.Id))
                    ||
                    _refactoringProviders
                        .SelectMany(rp => rp.GetRefactorings(range))
                        .Any()
                );
            }

            private bool IsFixable(string diagnosticId)
            {
                return _codeFixProviders.Any(cfp => cfp.CanFix(diagnosticId));
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

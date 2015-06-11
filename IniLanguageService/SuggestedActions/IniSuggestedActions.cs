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
    [ContentType(ContentTypes.Ini)]
    internal class IniSuggestedActionsProvider : ISuggestedActionsSourceProvider
    {
#pragma warning disable 649

        [Import]
        private IBufferTagAggregatorFactoryService aggregatorFactoryService;

        [ImportMany]
        private IEnumerable<ICodeRefactoringProvider> refactoringProviders;

#pragma warning restore 649


        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            return new IniSuggestedActions(
                textView, textBuffer,
                aggregatorFactoryService.CreateTagAggregator<IErrorTag>(textBuffer),
                refactoringProviders
            );
        }


        private sealed class IniSuggestedActions : ISuggestedActionsSource
        {
            public IniSuggestedActions(
                ITextView view, ITextBuffer buffer,
                ITagAggregator<IErrorTag> aggregator,
                IEnumerable<ICodeRefactoringProvider> refactoringProviders
            )
            {
                _view = view;
                _buffer = buffer;
                _aggregator = aggregator;
                _refactoringProviders = refactoringProviders;
            }

            private readonly ITextView _view;
            private readonly ITextBuffer _buffer;
            private readonly ITagAggregator<IErrorTag> _aggregator;
            private readonly IEnumerable<ICodeRefactoringProvider> _refactoringProviders;

            private static readonly IReadOnlyCollection<string> FixableDiagnosticIds = new []
            {
                "MultipleDeclarationsOfSection",
                "RedundantPropertyDeclaration",
            };
            
            public event EventHandler<EventArgs> SuggestedActionsChanged;

            public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
            {
                ITextBuffer buffer = range.Snapshot.TextBuffer;
                IniDocumentSyntax syntax = buffer.Properties.GetProperty<IniDocumentSyntax>("Syntax");
                
                yield return new SuggestedActionSet(
                    (
                        // code fixes
                        from tagSpan in _aggregator.GetTags(range)
                        from action in GetCodeFixesForDiagnostic(tagSpan)
                        select action
                    ).Union(
                        // code refactorings
                        from refactoring in _refactoringProviders
                        from action in refactoring.GetRefactorings(range)
                        select action.ToSuggestedAction()
                    ).ToArray()
                );
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

            private static bool IsFixable(string diagnosticId)
            {
                return FixableDiagnosticIds.Contains(diagnosticId);
            }

            private IEnumerable<ISuggestedAction> GetCodeFixesForDiagnostic(IMappingTagSpan<IErrorTag> tagSpan)
            {
                DiagnosticErrorTag tag = tagSpan.Tag as DiagnosticErrorTag;
                if (tag == null)
                    yield break;

                string diagnosticId = tag.Id;
                SnapshotSpan snapshotSpan = tagSpan.Span.GetSpans(_buffer).First();

                switch (diagnosticId)
                {
                    case "MultipleDeclarationsOfSection":
                        yield return new MergeDeclarationsIntoFirstSection(snapshotSpan);
                        break;

                    case "RedundantPropertyDeclaration":
                        yield return new RemoveRedundantPropertyDeclaration(snapshotSpan);
                        break;
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

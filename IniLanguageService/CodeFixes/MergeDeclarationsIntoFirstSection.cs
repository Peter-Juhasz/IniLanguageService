using IniLanguageService.CodeRefactorings;
using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace IniLanguageService.CodeFixes
{
    [Export(typeof(ICodeFixProvider))]
    internal sealed class MergeDeclarationsIntoFirstSection : ICodeFixProvider
    {
        private static readonly IReadOnlyCollection<string> FixableIds = new string[]
        {
            "MultipleDeclarationsOfSection"
        };

        public IEnumerable<string> FixableDiagnosticIds
        {
            get { return FixableIds; }
        }

        public IEnumerable<CodeAction> GetFixes(SnapshotSpan span)
        {
            ITextBuffer buffer = span.Snapshot.TextBuffer;
            IniDocumentSyntax syntax = buffer.Properties.GetProperty<IniDocumentSyntax>("Syntax");

            // find section
            IniSectionSyntax section = syntax.Sections
                .First(s => s.NameToken.Span.Span == span);

            string sectionName = section.NameToken.Value;

            // find first declaration
            IniSectionSyntax @base = syntax.Sections
                .First(s => s.NameToken.Value.Equals(sectionName, StringComparison.InvariantCultureIgnoreCase));

            sectionName = @base.NameToken.Value;

            yield return new CodeAction(
                $"Merge declarations into the first '{sectionName}' section",
                () => Fix(@base, section)
            );
        }
        
        public ITextEdit Fix(IniSectionSyntax @base, IniSectionSyntax current)
        {
            ITextBuffer buffer = @base.Document.Snapshot.TextBuffer;

            ITextEdit edit = buffer.CreateEdit();
            edit.Insert(
                @base.Span.End,
                new SnapshotSpan(
                    current.ClosingBracketToken.Span.Span.End,
                    current.Span.End
                ).GetText()
            );
            edit.Delete(current.Span);

            return edit;
        }
    }
}

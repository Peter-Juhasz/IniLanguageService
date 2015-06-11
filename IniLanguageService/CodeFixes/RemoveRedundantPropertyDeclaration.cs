using IniLanguageService.CodeRefactorings;
using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace IniLanguageService.CodeFixes
{
    [Export(typeof(ICodeFixProvider))]
    internal sealed class RemoveRedundantPropertyDeclaration : ICodeFixProvider
    {
        private static readonly IReadOnlyCollection<string> FixableIds = new string[]
        {
            "RedundantPropertyDeclaration"
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
            IniPropertySyntax property = syntax.Sections
                .SelectMany(s => s.Properties)
                .First(s => s.Span == span);

            yield return new CodeAction(
                $"Remove redundant property declaration '{property.NameToken.Value}'",
                () => Fix(property)
            );
        }
        
        public ITextEdit Fix(IniPropertySyntax property)
        {
            ITextBuffer buffer = property.Section.Document.Snapshot.TextBuffer;

            ITextEdit edit = buffer.CreateEdit();

            ITextSnapshotLine line = property.Span.Start.GetContainingLine();
            edit.Delete(new SnapshotSpan(line.Start, line.EndIncludingLineBreak));

            return edit;
        }
    }
}

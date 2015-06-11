using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace IniLanguageService.CodeRefactorings
{
    [Export(typeof(ICodeRefactoringProvider))]
    internal sealed class RemoveEmptyPropertyDeclaration : ICodeRefactoringProvider
    {
        public IEnumerable<CodeAction> GetRefactorings(SnapshotSpan span)
        {
            ITextBuffer buffer = span.Snapshot.TextBuffer;
            IniDocumentSyntax syntax = buffer.Properties.GetProperty<IniDocumentSyntax>("Syntax");

            // find applicable properties
            return
                from section in syntax.Sections
                where section.Span.IntersectsWith(span)
                from property in section.Properties
                where property.Span.IntersectsWith(span)
                where !property.DelimiterToken.IsMissing && property.ValueToken.IsMissing
                select new CodeAction(
                    $"Remove empty property declaration '{property.NameToken.Value}'", () => ApplyRefactoring(property)
                )
            ;
        }

        public ITextSnapshot ApplyRefactoring(IniPropertySyntax property)
        {
            ITextBuffer buffer = property.Section.Document.Snapshot.TextBuffer;

            using (ITextEdit edit = buffer.CreateEdit())
            {
                ITextSnapshotLine line = property.Span.Start.GetContainingLine();
                edit.Delete(new SnapshotSpan(line.Start, line.EndIncludingLineBreak));

                return edit.Apply();
            }
        }
    }
}

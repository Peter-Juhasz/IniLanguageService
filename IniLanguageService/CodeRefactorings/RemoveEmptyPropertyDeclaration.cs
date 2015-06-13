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
            SyntaxTree syntax = buffer.GetSyntaxTree();
            IniDocumentSyntax root = syntax.Root as IniDocumentSyntax;

            // find applicable properties
            return
                from section in root.Sections
                where section.Span.IntersectsWith(span)
                from property in section.Properties
                where property.Span.IntersectsWith(span)
                where !property.DelimiterToken.IsMissing && property.ValueToken.IsMissing
                select new CodeAction(
                    $"Remove empty property declaration '{property.NameToken.Value}'", () => ApplyRefactoring(property)
                )
            ;
        }

        public ITextEdit ApplyRefactoring(IniPropertySyntax property)
        {
            ITextBuffer buffer = property.Section.Document.Snapshot.TextBuffer;

            ITextEdit edit = buffer.CreateEdit();
            
            ITextSnapshotLine line = property.Span.Start.GetContainingLine();
            edit.Delete(new SnapshotSpan(line.Start, line.EndIncludingLineBreak));

            return edit;
        }
    }
}

﻿using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace IniLanguageService.CodeRefactorings
{
    [Export(typeof(ICodeRefactoringProvider))]
    internal sealed class RemoveEmptySection : ICodeRefactoringProvider
    {
        public IEnumerable<CodeAction> GetRefactorings(SnapshotSpan span)
        {
            ITextBuffer buffer = span.Snapshot.TextBuffer;
            SyntaxTree syntax = buffer.GetSyntaxTree();
            IniDocumentSyntax root = syntax.Root as IniDocumentSyntax;

            // find applicable sections
            return
                from section in root.Sections
                where section.Span.IntersectsWith(span)
                where !section.NameToken.IsMissing && !section.ClosingBracketToken.IsMissing
                where section.Properties.Count == 0
                select new CodeAction(
                    $"Remove empty section '{section.NameToken.Value}'", () => ApplyRefactoring(section)
                )
            ;
        }

        public ITextEdit ApplyRefactoring(IniSectionSyntax section)
        {
            ITextBuffer buffer = section.Document.Snapshot.TextBuffer;

            ITextEdit edit = buffer.CreateEdit();
            edit.Delete(section.Span);

            return edit;
        }
    }
}

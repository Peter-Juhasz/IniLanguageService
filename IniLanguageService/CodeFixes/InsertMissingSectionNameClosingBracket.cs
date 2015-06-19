using IniLanguageService.CodeRefactorings;
using IniLanguageService.Diagnostics;
using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace IniLanguageService.CodeFixes
{
    [Export(typeof(ICodeFixProvider))]
    internal sealed class InsertMissingSectionNameClosingBracket : ICodeFixProvider
    {
        private static readonly IReadOnlyCollection<string> FixableIds = new string[]
        {
            IniSectionSyntaxAnalyzer.MissingSectionNameClosingBracketMissing
        };

        public IEnumerable<string> FixableDiagnosticIds
        {
            get { return FixableIds; }
        }

        public IEnumerable<CodeAction> GetFixes(SnapshotSpan span)
        {
            ITextBuffer buffer = span.Snapshot.TextBuffer;
            SyntaxTree syntax = buffer.GetSyntaxTree();
            IniDocumentSyntax root = syntax.Root as IniDocumentSyntax;

            // find section
            IniSectionSyntax section = root.Sections
                .Where(s => !s.NameToken.IsMissing)
                .TakeWhile(s => s.NameToken.Span.Span.End <= span.Start)
                .Last();
            
            yield return new CodeAction(
                $"Fix syntax error: Insert missing '{IniSyntaxFacts.SectionNameClosingBracket}'",
                () => Fix(section)
            );
        }
        
        public ITextEdit Fix(IniSectionSyntax section)
        {
            ITextBuffer buffer = section.Document.Snapshot.TextBuffer;

            ITextEdit edit = buffer.CreateEdit();
            edit.Insert(section.NameToken.Span.Span.End, IniSyntaxFacts.SectionNameClosingBracket.ToString());

            return edit;
        }
    }
}

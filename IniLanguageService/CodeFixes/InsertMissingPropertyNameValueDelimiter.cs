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
    internal sealed class InsertMissingPropertyNameValueDelimiter : ICodeFixProvider
    {
        private static readonly IReadOnlyCollection<string> FixableIds = new string[]
        {
            IniPropertySyntaxAnalyzer.MissingPropertyNameValueDelimiter
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
            IniPropertySyntax property = root.Sections
                .SelectMany(s => s.Properties)
                .Where(p => !p.NameToken.IsMissing)
                .TakeWhile(p => p.NameToken.Span.Span.End <= span.Start)
                .Last();
            
            yield return new CodeAction(
                $"Fix syntax error: Insert missing '{IniSyntaxFacts.PropertyNameValueDelimiter}'",
                () => Fix(property)
            );
        }
        
        public ITextEdit Fix(IniPropertySyntax property)
        {
            ITextBuffer buffer = property.Section.Document.Snapshot.TextBuffer;

            ITextEdit edit = buffer.CreateEdit();
            edit.Insert(property.NameToken.Span.Span.End, IniSyntaxFacts.PropertyNameValueDelimiter.ToString());

            return edit;
        }
    }
}

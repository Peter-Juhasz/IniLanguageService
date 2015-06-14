using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace IniLanguageService.Diagnostics
{
    [ExportDiagnosticAnalyzer]
    internal sealed class MultipleDeclarationsOfSection : ISyntaxNodeAnalyzer<IniSectionSyntax>
    {
        public const string Id = nameof(MultipleDeclarationsOfSection);

        public IEnumerable<ITagSpan<IErrorTag>> Analyze(IniSectionSyntax section)
        {
            // check for duplicate sections
            string name = section.NameToken.Value;

            var other = section.Document.Sections
                .FirstOrDefault(
                    s => s.NameToken.Value.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                );

            if (other != section)
            {
                yield return new TagSpan<IErrorTag>(
                    section.NameToken.Span.Span,
                    new DiagnosticErrorTag(PredefinedErrorTypeNames.Warning, Id, $"Multiple declarations of section '{name}'")
                );
            }
        }
    }
}

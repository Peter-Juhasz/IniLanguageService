using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections.Generic;

namespace IniLanguageService.Diagnostics
{
    [ExportDiagnosticAnalyzer]
    internal sealed class IniSectionSyntaxAnalyzer : ISyntaxNodeAnalyzer<IniSectionSyntax>
    {
        public IEnumerable<ITagSpan<IErrorTag>> Analyze(IniSectionSyntax section)
        {
            // section name is missing
            if (section.NameToken.IsMissing)
            {
                yield return new TagSpan<IErrorTag>(
                    section.NameToken.Span.Span,
                    new ErrorTag(PredefinedErrorTypeNames.SyntaxError, "Section name expected")
                );
            }

            // closing bracket is missing
            else if (section.ClosingBracketToken.IsMissing)
            {
                yield return new TagSpan<IErrorTag>(
                    section.ClosingBracketToken.Span.Span,
                    new DiagnosticErrorTag(PredefinedErrorTypeNames.SyntaxError, "SectionNameClosingBracketMissing", "']' expected")
                );
            }
        }
    }
}

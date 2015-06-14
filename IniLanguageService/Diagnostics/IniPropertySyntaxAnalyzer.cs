using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections.Generic;

namespace IniLanguageService.Diagnostics
{
    [ExportDiagnosticAnalyzer]
    internal sealed class IniPropertySyntaxAnalyzer : ISyntaxNodeAnalyzer<IniPropertySyntax>
    {
        public IEnumerable<ITagSpan<IErrorTag>> Analyze(IniPropertySyntax property)
        {
            // delimiter missing
            if (property.DelimiterToken.IsMissing)
            {
                yield return new TagSpan<IErrorTag>(
                    property.DelimiterToken.Span.Span,
                    new DiagnosticErrorTag(PredefinedErrorTypeNames.SyntaxError, "PropertyNameValueDelimiterExpected", "'=' expected")
                );
            }
        }
    }
}

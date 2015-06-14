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
    internal sealed class RedundantPropertyDeclaration : ISyntaxNodeAnalyzer<IniPropertySyntax>
    {
        public const string Id = nameof(RedundantPropertyDeclaration);

        public IEnumerable<ITagSpan<IErrorTag>> Analyze(IniPropertySyntax property)
        {
            // check for duplicate properties
            string sectionName = property.Section.NameToken.Value;
            string name = property.NameToken.Value;

            var propertiesWithSameName = (
                from s in property.Section.Document.Sections
                where s.NameToken.Value.Equals(sectionName, StringComparison.InvariantCultureIgnoreCase)

                from p in s.Properties
                where p.NameToken.Value.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                select p
            ).ToList();

            // check for redundant declarations (name and value matches)
            string value = property.ValueToken.Value;

            var other = propertiesWithSameName
                .FirstOrDefault(p => p.ValueToken.Value == value);

            if (other != property)
            {
                yield return new TagSpan<IErrorTag>(
                    property.Span,
                    new DiagnosticErrorTag(PredefinedErrorTypeNames.Warning, Id, $"Redundant declaration of property '{name}'")
                );
            }

            // report only name equality
            else
            {
                other = propertiesWithSameName.FirstOrDefault();

                if (other != property)
                {
                    yield return new TagSpan<IErrorTag>(
                        property.NameToken.Span.Span,
                        new ErrorTag(PredefinedErrorTypeNames.Warning, $"Multiple declarations of property '{name}'")
                    );
                }
            }
        }
    }
}

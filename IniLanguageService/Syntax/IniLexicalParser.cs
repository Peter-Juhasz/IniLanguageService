using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace IniLanguageService.Syntax
{
    [Export("INI", typeof(ILexicalParser))]
    internal class IniLexicalParser : ILexicalParser
    {
        [ImportingConstructor]
        public IniLexicalParser(IClassificationTypeRegistryService registry)
        {
            _commentType = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
            _delimiterType = registry.GetClassificationType("INI/Delimiter");
            _propertyNameType = registry.GetClassificationType("INI/PropertyName");
            _propertyValueType = registry.GetClassificationType("INI/PropertyValue");
            _sectionNameType = registry.GetClassificationType("INI/SectionName");

        }

        private readonly IClassificationType _commentType;
        private readonly IClassificationType _sectionNameType;
        private readonly IClassificationType _delimiterType;
        private readonly IClassificationType _propertyNameType;
        private readonly IClassificationType _propertyValueType;


        public SyntaxTree Parse(ITextSnapshot snapshot)
        {
            IniDocumentSyntax root = new IniDocumentSyntax() { Snapshot = snapshot };

            List<SnapshotToken> leadingTrivia = new List<SnapshotToken>();
            IniSectionSyntax section = null;

            foreach (ITextSnapshotLine line in snapshot.Lines)
            {
                SnapshotPoint cursor = line.Start;
                snapshot.ReadWhiteSpace(ref cursor); // skip white space

                // skip blank lines
                if (cursor == line.End)
                    continue;

                char first = cursor.GetChar();

                // comment
                if (first == ';')
                {
                    SnapshotToken commentToken = new SnapshotToken(snapshot.ReadComment(ref cursor), _commentType);
                    leadingTrivia.Add(commentToken);
                }

                // section
                else if (first == '[')
                {
                    if (section != null)
                        root.Sections.Add(section);

                    SnapshotToken openingBracket = new SnapshotToken(snapshot.ReadDelimiter(ref cursor), _delimiterType);
                    snapshot.ReadWhiteSpace(ref cursor);
                    SnapshotToken name = new SnapshotToken(snapshot.ReadSectionName(ref cursor), _sectionNameType);
                    snapshot.ReadWhiteSpace(ref cursor);
                    SnapshotToken closingBracket = new SnapshotToken(snapshot.ReadDelimiter(ref cursor), _delimiterType);
                    snapshot.ReadWhiteSpace(ref cursor);
                    SnapshotToken commentToken = new SnapshotToken(snapshot.ReadComment(ref cursor), _commentType);

                    IList<SnapshotToken> trailingTrivia = new List<SnapshotToken>();
                    if (!commentToken.IsMissing)
                        trailingTrivia.Add(commentToken);

                    section = new IniSectionSyntax()
                    {
                        Document = root,
                        LeadingTrivia = leadingTrivia,
                        OpeningBracketToken = openingBracket,
                        NameToken = name,
                        ClosingBracketToken = closingBracket,
                        TrailingTrivia = trailingTrivia,
                    };
                    leadingTrivia = new List<SnapshotToken>();
                }

                // property
                else if (Char.IsLetter(first))
                {
                    SnapshotToken name = new SnapshotToken(snapshot.ReadPropertyName(ref cursor), _propertyNameType);
                    snapshot.ReadWhiteSpace(ref cursor);
                    SnapshotToken delimiter = new SnapshotToken(snapshot.ReadDelimiter(ref cursor), _delimiterType);
                    snapshot.ReadWhiteSpace(ref cursor);
                    SnapshotToken value = new SnapshotToken(snapshot.ReadPropertyValue(ref cursor), _propertyValueType);
                    snapshot.ReadWhiteSpace(ref cursor);
                    SnapshotToken commentToken = new SnapshotToken(snapshot.ReadComment(ref cursor), _commentType);

                    IList<SnapshotToken> trailingTrivia = new List<SnapshotToken>();
                    if (!commentToken.IsMissing)
                        trailingTrivia.Add(commentToken);

                    IniPropertySyntax property = new IniPropertySyntax()
                    {
                        Section = section,
                        LeadingTrivia = leadingTrivia,
                        NameToken = name,
                        DelimiterToken = delimiter,
                        ValueToken = value,
                        TrailingTrivia = trailingTrivia,
                    };
                    section.Properties.Add(property);
                    leadingTrivia = new List<SnapshotToken>();
                }

                // error
                else
                    ; // TODO: report error
            }

            if (section != null && leadingTrivia.Any())
                foreach (var trivia in leadingTrivia)
                    section.TrailingTrivia.Add(trivia);

            root.Sections.Add(section);

            return new SyntaxTree(snapshot, root);
        }
    }

    internal static class IniScanner
    {
        public static SnapshotSpan ReadDelimiter(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            var @char = point.GetChar();

            if (point.Position == snapshot.Length || (@char != '[' && @char != ']' && @char != '='))
                return new SnapshotSpan(point, 0);

            point = point + 1;
            return new SnapshotSpan(point - 1, 1);
        }
        public static SnapshotSpan ReadSectionName(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadToCommentOrLineEndWhile(ref point, c => c != ']');
        }
        public static SnapshotSpan ReadPropertyName(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadToCommentOrLineEndWhile(ref point, c => c != '=');
        }
        public static SnapshotSpan ReadPropertyValue(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadToCommentOrLineEndWhile(ref point, _ => true);
        }

        public static SnapshotSpan ReadComment(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            if (point.Position == snapshot.Length || point.GetChar() != ';')
                return new SnapshotSpan(point, 0);

            return snapshot.ReadToLineEndWhile(ref point, _ => true);
        }

        public static SnapshotSpan ReadToCommentOrLineEndWhile(this ITextSnapshot snapshot, ref SnapshotPoint point, Predicate<char> predicate)
        {
            return snapshot.ReadToLineEndWhile(ref point, c => c != ';' && predicate(c));
        }
    }
}

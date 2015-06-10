//------------------------------------------------------------------------------
// <copyright file="IniClassifier.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using IniLanguageService.Syntax;
using System.Linq;
using Microsoft.VisualStudio.Language.StandardClassification;

namespace IniLanguageService
{
    /// <summary>
    /// Classifier that classifies all text as an instance of the "IniClassifier" classification type.
    /// </summary>
    internal class IniClassifier : IClassifier
    {
        private readonly IClassificationType _commentType;
        private readonly IClassificationType _sectionNameType;
        private readonly IClassificationType _delimiterType;
        private readonly IClassificationType _propertyNameType;
        private readonly IClassificationType _propertyValueType;

        /// <summary>
        /// Initializes a new instance of the <see cref="IniClassifier"/> class.
        /// </summary>
        /// <param name="registry">Classification registry.</param>
        internal IniClassifier(ITextBuffer buffer, IClassificationTypeRegistryService registry)
        {
            _commentType = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
            _delimiterType = registry.GetClassificationType("INI/Delimiter");
            _propertyNameType = registry.GetClassificationType("INI/PropertyName");
            _propertyValueType = registry.GetClassificationType("INI/PropertyValue");
            _sectionNameType = registry.GetClassificationType("INI/SectionName");
            
            buffer.ChangedHighPriority += OnBufferChanged;

            IniDocumentSyntax syntax = Parse(buffer.CurrentSnapshot);
            buffer.Properties.AddProperty("Syntax", syntax);
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            ITextBuffer buffer = sender as ITextBuffer;
            
            // reparse
            IniDocumentSyntax syntax = buffer.Properties.GetOrCreateSingletonProperty<IniDocumentSyntax>("Syntax", () => Parse(buffer.CurrentSnapshot));
            if (syntax.Snapshot != buffer.CurrentSnapshot)
            {
                buffer.Properties.RemoveProperty("Syntax");
                syntax = Parse(buffer.CurrentSnapshot);
                buffer.Properties.AddProperty("Syntax", syntax);
            }

            // format
            if (e.Changes.Count == 1)
            {
                ITextChange change = e.Changes.Single();

                if (change.OldLength == 0 && change.NewLength == 1)
                {
                    // format on ']'
                    if (change.NewText == "]")
                    {
                        IniSectionSyntax section = syntax.Sections
                            .FirstOrDefault(s => s.ClosingBracketToken.Span.Span == change.NewSpan);

                        if (section != null)
                        {
                            // remove unnecessary whitespace
                            using (ITextEdit format = buffer.CreateEdit())
                            {
                                if (section.OpeningBracketToken.Span.Span.End != section.NameToken.Span.Span.Start)
                                    format.Delete(new SnapshotSpan(section.OpeningBracketToken.Span.Span.End, section.NameToken.Span.Span.Start));

                                if (section.NameToken.Span.Span.End != section.ClosingBracketToken.Span.Span.Start)
                                    format.Delete(new SnapshotSpan(section.NameToken.Span.Span.End, section.ClosingBracketToken.Span.Span.Start));

                                format.Apply();
                            }
                        }
                    }

                    // format on '='
                    else if (change.NewText == "=")
                    {
                        IniPropertySyntax property = syntax.Sections
                            .SelectMany(s => s.Properties)
                            .FirstOrDefault(p => p.DelimiterToken.Span.Span == change.NewSpan);

                        if (property != null)
                        {
                            // reference point is section opening '['
                            SnapshotPoint referencePoint = property.Section.OpeningBracketToken.Span.Span.Start;

                            // find property before
                            IniPropertySyntax before = property.Section.Properties
                                .TakeWhile(p => p != property)
                                .LastOrDefault();

                            // override reference point if found property before
                            if (before != null)
                                referencePoint = before.NameToken.Span.Span.Start;

                            // compare
                            ITextSnapshotLine referenceLine = referencePoint.GetContainingLine();
                            ITextSnapshotLine line = property.DelimiterToken.Span.Span.End.GetContainingLine();

                            SnapshotSpan referenceIndent = new SnapshotSpan(referenceLine.Start, referencePoint);
                            SnapshotSpan indent = new SnapshotSpan(line.Start, property.NameToken.Span.Span.Start);

                            if (referenceIndent.GetText() != indent.GetText())
                            {
                                using (ITextEdit edit = buffer.CreateEdit())
                                {
                                    edit.Replace(indent, referenceIndent.GetText());

                                    edit.Apply();
                                }
                            }
                        }
                    }
                }
            }
        }

        protected IniDocumentSyntax Parse(ITextSnapshot snapshot)
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

            return root;
        }
        

#pragma warning disable 67

        /// <summary>
        /// An event that occurs when the classification of a span of text has changed.
        /// </summary>
        /// <remarks>
        /// This event gets raised if a non-text change would affect the classification in some way,
        /// for example typing /* would cause the classification to change in C# without directly
        /// affecting the span.
        /// </remarks>
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

#pragma warning restore 67

        /// <summary>
        /// Gets all the <see cref="ClassificationSpan"/> objects that intersect with the given range of text.
        /// </summary>
        /// <remarks>
        /// This method scans the given SnapshotSpan for potential matches for this classification.
        /// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
        /// </remarks>
        /// <param name="span">The span currently being classified.</param>
        /// <returns>A list of ClassificationSpans that represent spans identified to be of this classification.</returns>
        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            IniDocumentSyntax syntax = span.Snapshot.TextBuffer.Properties.GetOrCreateSingletonProperty<IniDocumentSyntax>("Syntax", () => Parse(span.Snapshot));

            return (
                from token in syntax.GetTokens()
                where !token.IsMissing
                where token.Span.Span.IntersectsWith(span)
                select token.Span
            ).ToList();
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

    internal static class CommonScanner
    {
        public static SnapshotSpan ReadWhiteSpace(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadToLineEndWhile(ref point, Char.IsWhiteSpace, rewindWhiteSpace: false);
        }

        public static SnapshotSpan ReadToLineEndWhile(this ITextSnapshot snapshot, ref SnapshotPoint point, Predicate<char> predicate, bool rewindWhiteSpace = true)
        {
            SnapshotPoint start = point;

            while (
                point.Position < snapshot.Length &&
                point.GetChar() != '\n' && point.GetChar() != '\r' &&
                predicate(point.GetChar())
            )
                point = point + 1;

            if (rewindWhiteSpace)
            {
                while (
                    point - 1 >= start &&
                    Char.IsWhiteSpace((point - 1).GetChar())
                )
                    point = point - 1;
            }

            return new SnapshotSpan(start, point);
        }
    }
}

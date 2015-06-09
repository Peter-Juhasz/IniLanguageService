﻿//------------------------------------------------------------------------------
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

            IniDocumentSyntax syntax = Reparse(buffer.CurrentSnapshot);
            buffer.Properties.AddProperty("Syntax", syntax);
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            ITextBuffer buffer = sender as ITextBuffer;
            
            // reparse
            IniDocumentSyntax syntax = buffer.Properties.GetOrCreateSingletonProperty<IniDocumentSyntax>("Syntax", () => Reparse(buffer.CurrentSnapshot));
            if (syntax.Snapshot != buffer.CurrentSnapshot)
            {
                buffer.Properties.RemoveProperty("Syntax");
                syntax = Reparse(buffer.CurrentSnapshot);
                buffer.Properties.AddProperty("Syntax", syntax);
            }

            // format
            if (e.Changes.Count == 1)
            {
                ITextChange change = e.Changes.Single();

                // format on ']'
                if (change.OldLength == 0 && change.NewText == "]")
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
            }
        }

        private IniDocumentSyntax Reparse(ITextSnapshot snapshot)
        {
            IniDocumentSyntax root = new IniDocumentSyntax() { Snapshot = snapshot };

            List<SnapshotToken> leadingTrivia = new List<SnapshotToken>();
            IniSectionSyntax section = null;

            foreach (ITextSnapshotLine line in snapshot.Lines)
            {
                SnapshotPoint cursor = line.Start;
                snapshot.ReadWhiteSpace(ref cursor); // skip white space

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

                    section = new IniSectionSyntax()
                    {
                        Document = root,
                        LeadingTrivia = leadingTrivia,
                        OpeningBracketToken = openingBracket,
                        NameToken = name,
                        ClosingBracketToken = closingBracket,
                        TrailingTrivia = commentToken,
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

                    IniPropertySyntax property = new IniPropertySyntax()
                    {
                        Section = section,
                        LeadingTrivia = leadingTrivia,
                        NameToken = name,
                        DelimiterToken = delimiter,
                        ValueToken = value,
                        TrailingTrivia = commentToken,
                    };
                    section.Properties.Add(property);
                    leadingTrivia = new List<SnapshotToken>();
                }

                // error
                else
                    ;
            }

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
            IniDocumentSyntax syntax = span.Snapshot.TextBuffer.Properties.GetOrCreateSingletonProperty<IniDocumentSyntax>("Syntax", () => Reparse(span.Snapshot));

            return syntax.GetTokens()
                .Where(t => !t.IsMissing)
                .Select(t => t.Span)
                .Where(s => s.Span.IntersectsWith(span))
                .ToList();
        }
    }


    public static class Scanner
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

        public static SnapshotSpan ReadWhiteSpace(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadToLineEndWhile(ref point, Char.IsWhiteSpace, rewindWhiteSpace: false);
        }

        public static SnapshotSpan ReadToCommentOrLineEndWhile(this ITextSnapshot snapshot, ref SnapshotPoint point, Predicate<char> predicate)
        {
            return snapshot.ReadToLineEndWhile(ref point, c => c != ';' && predicate(c));
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

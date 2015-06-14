//------------------------------------------------------------------------------
// <copyright file="IniClassifier.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IniLanguageService
{
    /// <summary>
    /// Classifier that classifies all text as an instance of the "IniClassifier" classification type.
    /// </summary>
    internal sealed class IniClassifier : IClassifier
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
        public IniClassifier(ITextBuffer buffer, ISyntacticParser syntacticParser, IClassificationTypeRegistryService registry)
        {
            _syntacticParser = buffer.Properties.GetOrCreateSingletonProperty<ISyntacticParser>(() => syntacticParser);

            _commentType = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
            _delimiterType = registry.GetClassificationType("INI/Delimiter");
            _propertyNameType = registry.GetClassificationType("INI/PropertyName");
            _propertyValueType = registry.GetClassificationType("INI/PropertyValue");
            _sectionNameType = registry.GetClassificationType("INI/SectionName");
            
            buffer.ChangedHighPriority += OnBufferChanged;
        }

        private readonly ISyntacticParser _syntacticParser;

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            ITextBuffer buffer = sender as ITextBuffer;
            
            // format
            if (e.Changes.Count == 1)
            {
                ITextChange change = e.Changes.Single();

                if (change.OldLength == 0 && change.NewLength == 1)
                {
                    // format on ']'
                    if (change.NewText == "]")
                    {
                        SyntaxTree syntaxTree = buffer.GetSyntaxTree();
                        IniDocumentSyntax root = syntaxTree.Root as IniDocumentSyntax;

                        IniSectionSyntax section = root.Sections
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
                        SyntaxTree syntaxTree = buffer.GetSyntaxTree();
                        IniDocumentSyntax root = syntaxTree.Root as IniDocumentSyntax;

                        IniPropertySyntax property = root.Sections
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
            SyntaxTree syntaxTree = span.Snapshot.GetSyntaxTree();

            return (
                from token in syntaxTree.Root.GetTokens()
                where !token.IsMissing
                where token.Span.Span.IntersectsWith(span)
                select token.Span
            ).ToList();
        }
    }
}

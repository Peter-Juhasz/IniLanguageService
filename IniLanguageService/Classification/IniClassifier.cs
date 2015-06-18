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
            buffer.Properties.AddProperty(typeof(ISyntacticParser), syntacticParser);

            _commentType = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
            _delimiterType = registry.GetClassificationType("INI/Delimiter");
            _propertyNameType = registry.GetClassificationType("INI/PropertyName");
            _propertyValueType = registry.GetClassificationType("INI/PropertyValue");
            _sectionNameType = registry.GetClassificationType("INI/SectionName");
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

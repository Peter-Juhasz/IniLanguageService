﻿using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using IniLanguageService.Syntax;

namespace IniLanguageService
{
    /// <summary>
    /// Classifier provider. It adds the classifier to the set of classifiers.
    /// </summary>
    [Export(typeof(IClassifierProvider))]
    [ContentType(ContentTypes.Ini)] // This classifier applies to all text files.
    [Order(Before = Priority.High)]
    internal class IniClassifierProvider : IClassifierProvider
    {
        // Disable "Field is never assigned to..." compiler's warning. Justification: the field is assigned by MEF.
#pragma warning disable 649

        /// <summary>
        /// Classification registry to be used for getting a reference
        /// to the custom classification type later.
        /// </summary>
        [Import]
        private IClassificationTypeRegistryService classificationRegistry;

        [Import("INI")]
        private ILexicalParser lexicalParser;

#pragma warning restore 649


        /// <summary>
        /// Gets a classifier for the given text buffer.
        /// </summary>
        /// <param name="buffer">The <see cref="ITextBuffer"/> to classify.</param>
        /// <returns>A classifier for the text buffer, or null if the provider cannot do so in its current state.</returns>
        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty<IniClassifier>(
                creator: () => new IniClassifier(buffer, lexicalParser, this.classificationRegistry)
            );
        }
    }
}

//------------------------------------------------------------------------------
// <copyright file="IniClassifierClassificationDefinition.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace IniLanguageService
{
    /// <summary>
    /// Classification type definition export for IniClassifier
    /// </summary>
    internal static class IniClassifierClassificationDefinitions
    {
#pragma warning disable 169
        
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("INI/Delimiter")]
        private static ClassificationTypeDefinition delimiter;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("INI/SectionName")]
        private static ClassificationTypeDefinition sectionName;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("INI/PropertyName")]
        private static ClassificationTypeDefinition propertyName;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("INI/PropertyValue")]
        private static ClassificationTypeDefinition propertyValue;

#pragma warning restore 169
    }
}

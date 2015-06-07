//------------------------------------------------------------------------------
// <copyright file="IniClassifierFormat.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace IniLanguageService
{
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "INI/Delimiter")]
    [Name("INI/Delimiter")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class IniDelimiterClassificationFormat : ClassificationFormatDefinition
    {
        public IniDelimiterClassificationFormat()
        {
            this.DisplayName = "INI Delimiter"; // Human readable version of the name
            this.ForegroundColor = Colors.Blue;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "INI/SectionName")]
    [Name("INI/SectionName")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class IniSectionNameClassificationFormat : ClassificationFormatDefinition
    {
        public IniSectionNameClassificationFormat()
        {
            this.DisplayName = "INI Section Name"; // Human readable version of the name
            this.ForegroundColor = Colors.Red;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "INI/PropertyName")]
    [Name("INI/PropertyName")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class IniPropertyNameClassificationFormat : ClassificationFormatDefinition
    {
        public IniPropertyNameClassificationFormat()
        {
            this.DisplayName = "INI Property Name"; // Human readable version of the name
            this.ForegroundColor = Colors.Maroon;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "INI/PropertyValue")]
    [Name("INI/PropertyValue")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class IniPropertyValueClassificationFormat : ClassificationFormatDefinition
    {
        public IniPropertyValueClassificationFormat()
        {
            this.DisplayName = "INI Property Value"; // Human readable version of the name
            this.ForegroundColor = Colors.Blue;
        }
    }
}

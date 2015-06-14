//------------------------------------------------------------------------------
// <copyright file="IniClassifierClassificationDefinition.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace IniLanguageService
{
    /// <summary>
    /// Classification type definition export for IniClassifier
    /// </summary>
    internal static class IniContentTypeDefinition
    {
        // This disables "The field is never used" compiler's warning. Justification: the field is used by MEF.
#pragma warning disable 649

        [Export]
        [Name("INI")]
        [BaseDefinition("code")]
        internal static ContentTypeDefinition iniContentTypeDefinition;

        [Export]
        [FileExtension(".ini")]
        [ContentType(IniContentTypeNames.Ini)]
        internal static FileExtensionToContentTypeDefinition iniFileExtensionToContentTypeDefinition;

#pragma warning restore 649
    }
}

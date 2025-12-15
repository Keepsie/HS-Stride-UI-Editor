// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using HS.Stride.UI.Editor.ViewModels;
using HS.Stride.Editor.Toolkit.Core;

namespace HS.Stride.UI.Editor.Core.Services.Interfaces
{
    /// <summary>
    /// Result of loading a document
    /// </summary>
    public record LoadDocumentResult(
        List<UIElementViewModel> RootElements,
        double DesignWidth,
        double DesignHeight);

    /// <summary>
    /// Service for handling file operations (New, Open, Save)
    /// </summary>
    public interface IFileOperationService
    {
        /// <summary>
        /// Current file path (null if not saved yet)
        /// </summary>
        string? CurrentFilePath { get; }

        /// <summary>
        /// Whether there are unsaved changes
        /// </summary>
        bool HasUnsavedChanges { get; set; }

        /// <summary>
        /// Set the connected Stride project (required for all operations)
        /// </summary>
        void SetProject(StrideProject? project);

        /// <summary>
        /// Create a new blank UI page with root Grid container
        /// </summary>
        List<UIElementViewModel> CreateNewDocument(double designWidth, double designHeight);

        /// <summary>
        /// Load a UI page from a .sduipage file
        /// </summary>
        LoadDocumentResult LoadDocument(string filePath, double defaultWidth, double defaultHeight);

        /// <summary>
        /// Save the current document (must have CurrentFilePath set)
        /// </summary>
        void SaveDocument(List<UIElementViewModel> rootElements, double designWidth, double designHeight);

        /// <summary>
        /// Save the document to a new file path
        /// </summary>
        string SaveDocumentAs(List<UIElementViewModel> rootElements, string filePath, double designWidth, double designHeight);
    }
}

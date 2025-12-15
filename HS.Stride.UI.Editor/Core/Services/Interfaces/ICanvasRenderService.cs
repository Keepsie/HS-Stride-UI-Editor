// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections.ObjectModel;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Core.Services.Interfaces
{
    /// <summary>
    /// Service for managing canvas rendering operations
    /// </summary>
    public interface ICanvasRenderService
    {
        /// <summary>
        /// Render all elements from the root collection
        /// </summary>
        void RenderAllElements(ObservableCollection<UIElementViewModel> rootElements);

        /// <summary>
        /// Render a single element and its children recursively
        /// </summary>
        void RenderElement(UIElementViewModel element);

        /// <summary>
        /// Remove an element's visual representation from the canvas
        /// </summary>
        void RemoveElementVisual(UIElementViewModel element);

        /// <summary>
        /// Get a visual element by ID (returns null if not found)
        /// </summary>
        object? GetVisual(string elementId);

        /// <summary>
        /// Check if a visual exists for an element
        /// </summary>
        bool HasVisual(string elementId);

        /// <summary>
        /// Clear all visuals from the canvas
        /// </summary>
        void ClearAll();
    }
}

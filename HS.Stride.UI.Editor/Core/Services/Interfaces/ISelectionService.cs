// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Core.Services.Interfaces
{
    /// <summary>
    /// Service for managing element selection
    /// </summary>
    public interface ISelectionService
    {
        /// <summary>
        /// Get currently selected elements
        /// </summary>
        IReadOnlyList<UIElementViewModel> SelectedElements { get; }

        /// <summary>
        /// Get the primary selected element (first in selection)
        /// </summary>
        UIElementViewModel? PrimarySelection { get; }

        /// <summary>
        /// Select an element (optionally add to existing selection)
        /// </summary>
        void Select(UIElementViewModel element, bool addToSelection = false);

        /// <summary>
        /// Clear all selection
        /// </summary>
        void ClearSelection();

        /// <summary>
        /// Check if an element is selected
        /// </summary>
        bool IsSelected(UIElementViewModel element);
    }
}

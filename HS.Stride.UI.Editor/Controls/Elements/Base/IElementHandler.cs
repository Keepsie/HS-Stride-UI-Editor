// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows.Media;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Controls.Elements
{
    /// <summary>
    /// Interface for element-specific visual handlers.
    /// Each UI element type has a handler that manages its visual representation.
    /// </summary>
    public interface IElementHandler
    {
        /// <summary>
        /// The element type this handler manages (e.g., "Button", "TextBlock")
        /// </summary>
        string ElementType { get; }

        /// <summary>
        /// Default width for new elements of this type
        /// </summary>
        double DefaultWidth { get; }

        /// <summary>
        /// Default height for new elements of this type
        /// </summary>
        double DefaultHeight { get; }

        /// <summary>
        /// Default background color for this element type
        /// </summary>
        Color DefaultBackgroundColor { get; }

        /// <summary>
        /// Creates the initial visual elements for this element type
        /// </summary>
        void InitializeVisual(UIElementVisual visual, UIElementViewModel viewModel);

        /// <summary>
        /// Updates element-specific content (text, images, etc.)
        /// </summary>
        void UpdateContent(UIElementVisual visual, UIElementViewModel viewModel);

        /// <summary>
        /// Called when element size changes - update size-dependent visuals
        /// </summary>
        void UpdateSize(UIElementVisual visual, UIElementViewModel viewModel);

        /// <summary>
        /// Gets the fill brush for this element type (default appearance)
        /// </summary>
        Brush GetFillBrush(UIElementViewModel viewModel);

        /// <summary>
        /// Gets the hint brush for showing element boundaries in editor
        /// </summary>
        Brush GetHintBrush();

        /// <summary>
        /// Returns property categories relevant to this element type
        /// </summary>
        IEnumerable<string> GetPropertyCategories();

        /// <summary>
        /// Layout children if this is a container element
        /// </summary>
        void LayoutChildren(UIElementVisual visual, UIElementViewModel viewModel);

        /// <summary>
        /// Handle property change for element-specific properties
        /// Returns true if the property was handled
        /// </summary>
        bool HandlePropertyChange(UIElementVisual visual, UIElementViewModel viewModel, string propertyName);
    }
}

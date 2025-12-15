// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections.ObjectModel;
using HS.Stride.UI.Editor.Core.Abstractions;
using HS.Stride.UI.Editor.Models;
using HS.Stride.UI.Editor.Models.Commands;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Core.Services.Interfaces
{
    /// <summary>
    /// Drop position and parent information
    /// </summary>
    public record DropInfo(bool IsValid, UIElementViewModel? NewParent, int InsertIndex);

    /// <summary>
    /// Service for handling drag-drop operations
    /// </summary>
    public interface IDragDropService
    {
        /// <summary>
        /// Create element from UI library factory drop on canvas
        /// </summary>
        CreateElementCommand CreateElementFromFactory(
            UIElementFactory factory,
            Point2D dropPosition,
            UIElementViewModel? parent,
            ObservableCollection<UIElementViewModel> rootElements,
            Action<UIElementViewModel> renderElement,
            Action<UIElementViewModel> removeElementVisual,
            Action<UIElementViewModel> selectElement);

        /// <summary>
        /// Create element from asset drop on canvas
        /// </summary>
        CreateElementCommand? CreateElementFromAsset(
            AssetItem asset,
            Point2D dropPosition,
            UIElementViewModel? parent,
            ObservableCollection<UIElementViewModel> rootElements,
            Action<UIElementViewModel> renderElement,
            Action<UIElementViewModel> removeElementVisual,
            Action<UIElementViewModel> selectElement,
            double? width = null,
            double? height = null);

        /// <summary>
        /// Create element from UI library factory drop on tree view
        /// </summary>
        CreateElementCommand CreateElementFromFactoryOnTree(
            UIElementFactory factory,
            UIElementViewModel targetElement,
            ObservableCollection<UIElementViewModel> rootElements,
            Action<UIElementViewModel> renderElement,
            Action<UIElementViewModel> removeElementVisual,
            Action<UIElementViewModel> selectElement);

        /// <summary>
        /// Create element from asset drop on tree view
        /// </summary>
        CreateElementCommand? CreateElementFromAssetOnTree(
            AssetItem asset,
            UIElementViewModel targetElement,
            ObservableCollection<UIElementViewModel> rootElements,
            Action<UIElementViewModel> renderElement,
            Action<UIElementViewModel> removeElementVisual,
            Action<UIElementViewModel> selectElement,
            double? width = null,
            double? height = null);

        /// <summary>
        /// Validate if reparenting is allowed
        /// </summary>
        bool IsValidReparent(UIElementViewModel element, UIElementViewModel? newParent);

        /// <summary>
        /// Calculate drop info for reparenting in tree view
        /// </summary>
        DropInfo CalculateReparentDropInfo(
            UIElementViewModel draggedElement,
            UIElementViewModel targetElement,
            Point2D relativePosition,
            double targetHeight,
            bool droppedOnRoot);

        /// <summary>
        /// Create reparent command
        /// </summary>
        ReparentElementCommand CreateReparentCommand(
            UIElementViewModel element,
            UIElementViewModel newParent,
            int insertIndex,
            ObservableCollection<UIElementViewModel> rootElements,
            Action renderAllElements,
            Func<(double, double)> transformCoordinates);

        /// <summary>
        /// Clamp position to artboard bounds
        /// </summary>
        Point2D ClampToArtboardBounds(Point2D position, double artboardWidth, double artboardHeight, double elementWidth = 200, double elementHeight = 200);
    }
}

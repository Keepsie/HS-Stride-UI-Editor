// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections.ObjectModel;
using HS.Stride.UI.Editor.Core.Abstractions;
using HS.Stride.UI.Editor.Core.Services.Interfaces;
using HS.Stride.UI.Editor.Models;
using HS.Stride.UI.Editor.Models.Commands;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Core.Services
{
    /// <summary>
    /// Handles drag-drop business logic: creating elements from drops, position calculations, and command generation
    /// </summary>
    public class DragDropService : IDragDropService
    {
        private readonly Func<string, string> _generateElementName;

        public DragDropService(Func<string, string> generateElementName)
        {
            _generateElementName = generateElementName;
        }

        #region Canvas Drop Logic

        /// <summary>
        /// Create element from UI library factory drop on canvas
        /// </summary>
        public CreateElementCommand CreateElementFromFactory(
            UIElementFactory factory,
            Point2D dropPosition,
            UIElementViewModel? parent,
            ObservableCollection<UIElementViewModel> rootElements,
            Action<UIElementViewModel> renderElement,
            Action<UIElementViewModel> removeElementVisual,
            Action<UIElementViewModel> selectElement)
        {
            var newElement = new UIElementViewModel(
                _generateElementName(factory.ElementType),
                factory.ElementType)
            {
                X = dropPosition.X,
                Y = dropPosition.Y,
                Width = 200,
                Height = 100
            };

            return new CreateElementCommand(
                newElement,
                parent,
                rootElements,
                renderElement,
                removeElementVisual,
                selectElement);
        }

        /// <summary>
        /// Create element from asset drop on canvas
        /// </summary>
        public CreateElementCommand? CreateElementFromAsset(
            AssetItem asset,
            Point2D dropPosition,
            UIElementViewModel? parent,
            ObservableCollection<UIElementViewModel> rootElements,
            Action<UIElementViewModel> renderElement,
            Action<UIElementViewModel> removeElementVisual,
            Action<UIElementViewModel> selectElement,
            double? width = null,
            double? height = null)
        {
            UIElementViewModel? newElement = null;

            switch (asset.Type)
            {
                case "Texture":
                case "SpriteSheet":
                    newElement = new UIElementViewModel(
                        _generateElementName("Image"),
                        "ImageElement")
                    {
                        X = dropPosition.X,
                        Y = dropPosition.Y,
                        Width = width ?? 200,
                        Height = height ?? 200,
                        ImageSource = asset.Name,
                        ImageAssetReference = asset.AssetReference, // Store actual AssetReference for export
                        ImageAssetType = asset.Type // "Texture" or "SpriteSheet"
                    };
                    break;
            }

            if (newElement == null)
                return null;

            return new CreateElementCommand(
                newElement,
                parent,
                rootElements,
                renderElement,
                removeElementVisual,
                selectElement);
        }

        #endregion

        #region Tree View Drop Logic

        /// <summary>
        /// Create element from UI library factory drop on tree view
        /// </summary>
        public CreateElementCommand CreateElementFromFactoryOnTree(
            UIElementFactory factory,
            UIElementViewModel targetParent,
            ObservableCollection<UIElementViewModel> rootElements,
            Action<UIElementViewModel> renderElement,
            Action<UIElementViewModel> removeElementVisual,
            Action<UIElementViewModel> selectElement)
        {
            var newElement = new UIElementViewModel(
                _generateElementName(factory.ElementType),
                factory.ElementType)
            {
                X = 0,
                Y = 0,
                Width = 200,
                Height = 100
            };

            return new CreateElementCommand(
                newElement,
                targetParent,
                rootElements,
                renderElement,
                removeElementVisual,
                selectElement);
        }

        /// <summary>
        /// Create element from asset drop on tree view
        /// </summary>
        public CreateElementCommand? CreateElementFromAssetOnTree(
            AssetItem asset,
            UIElementViewModel targetParent,
            ObservableCollection<UIElementViewModel> rootElements,
            Action<UIElementViewModel> renderElement,
            Action<UIElementViewModel> removeElementVisual,
            Action<UIElementViewModel> selectElement,
            double? width = null,
            double? height = null)
        {
            UIElementViewModel? newElement = null;

            switch (asset.Type)
            {
                case "Texture":
                case "SpriteSheet":
                    newElement = new UIElementViewModel(
                        _generateElementName("Image"),
                        "ImageElement")
                    {
                        X = 0,
                        Y = 0,
                        Width = width ?? 200,
                        Height = height ?? 200,
                        ImageSource = asset.Name,
                        ImageAssetReference = asset.AssetReference, // Store actual AssetReference for export
                        ImageAssetType = asset.Type // "Texture" or "SpriteSheet"
                    };
                    break;
            }

            if (newElement == null)
                return null;

            return new CreateElementCommand(
                newElement,
                targetParent,
                rootElements,
                renderElement,
                removeElementVisual,
                selectElement);
        }

        #endregion

        #region Reparenting Logic

        /// <summary>
        /// Determine drop behavior for tree view reparenting (Before/Inside/After)
        /// </summary>
        public DropInfo CalculateReparentDropInfo(
            UIElementViewModel draggedElement,
            UIElementViewModel targetElement,
            Point2D relativePosition,
            double targetHeight,
            bool droppedOnRoot)
        {
            // Check if dragging on descendant (not allowed)
            if (targetElement.IsDescendantOf(draggedElement))
            {
                return new DropInfo(false, null, -1);
            }

            // Check if dragging on self (not allowed)
            if (draggedElement == targetElement)
            {
                return new DropInfo(false, null, -1);
            }

            UIElementViewModel newParent = targetElement;
            int newIndex = -1; // -1 means append

            if (!droppedOnRoot)
            {
                // Threshold for Before/After (top/bottom 20%) - reduced from 25% to make "inside" zone larger
                double edgeThreshold = targetHeight * 0.20;

                if (relativePosition.Y < edgeThreshold)
                {
                    // Insert Before target (as sibling)
                    if (targetElement.Parent != null)
                    {
                        newParent = targetElement.Parent;
                        int targetIndex = newParent.Children.IndexOf(targetElement);
                        newIndex = Math.Max(0, targetIndex);
                    }
                    // If target has no parent (is root element), just insert inside the target instead
                    // This makes it easier to drop onto container elements
                }
                else if (relativePosition.Y > targetHeight - edgeThreshold)
                {
                    // Insert After target (as sibling)
                    if (targetElement.Parent != null)
                    {
                        newParent = targetElement.Parent;
                        int targetIndex = newParent.Children.IndexOf(targetElement);
                        newIndex = Math.Max(0, targetIndex + 1);
                    }
                    // If target has no parent (is root element), just insert inside the target instead
                }
                // else: Insert Inside (middle 60% of the item height)
                // newParent is already set to targetElement, newIndex is -1 (append)
            }

            // Final sanity check
            if (draggedElement == newParent)
            {
                return new DropInfo(false, null, -1);
            }

            // Adjust index for same-parent reordering
            // When moving within the same parent, removing the element first shifts indices
            // If dragged element is before the target index, we need to decrement by 1
            if (newIndex >= 0 && draggedElement.Parent == newParent)
            {
                int currentIndex = newParent.Children.IndexOf(draggedElement);
                if (currentIndex >= 0 && currentIndex < newIndex)
                {
                    newIndex--;
                }
            }

            return new DropInfo(true, newParent, newIndex);
        }

        /// <summary>
        /// Create reparent command
        /// </summary>
        public ReparentElementCommand CreateReparentCommand(
            UIElementViewModel element,
            UIElementViewModel newParent,
            int insertIndex,
            ObservableCollection<UIElementViewModel> rootElements,
            Action renderAllElements,
            Func<(double, double)> transformCoordinates)
        {
            return new ReparentElementCommand(
                element,
                newParent,
                rootElements,
                renderAllElements, // Actually re-render after reparenting!
                (el, oldP, newP) => transformCoordinates(), // Adapt transform signature
                insertIndex);
        }

        #endregion

        #region Position Calculations

        /// <summary>
        /// Clamp drop position to artboard bounds
        /// </summary>
        public Point2D ClampToArtboardBounds(Point2D dropPosition, double designWidth, double designHeight, double elementWidth = 200, double elementHeight = 200)
        {
            return new Point2D(
                Math.Max(0, Math.Min(dropPosition.X, designWidth - elementWidth)),
                Math.Max(0, Math.Min(dropPosition.Y, designHeight - elementHeight))
            );
        }

        #endregion

        #region Validation

        /// <summary>
        /// Check if reparenting is valid (not self, not descendant)
        /// </summary>
        public bool IsValidReparent(UIElementViewModel draggedElement, UIElementViewModel? targetElement)
        {
            // Reparenting to null is valid (removes element from parent, makes it root)
            if (targetElement == null)
                return true;

            if (draggedElement == targetElement)
                return false;

            if (targetElement.IsDescendantOf(draggedElement))
                return false;

            return true;
        }

        #endregion
    }

    /// <summary>
    /// Information about a reparent drop operation
    /// </summary>
    public class ReparentDropInfo
    {
        public bool IsValid { get; set; }
        public UIElementViewModel? NewParent { get; set; }
        public int InsertIndex { get; set; } = -1; // -1 means append
    }
}

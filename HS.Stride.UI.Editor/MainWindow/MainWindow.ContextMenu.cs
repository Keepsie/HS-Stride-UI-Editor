// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using HS.Stride.UI.Editor.Controls;
using HS.Stride.UI.Editor.Models.Commands;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor
{
    /// <summary>
    /// MainWindow partial class - Context menu handlers (delete, duplicate, group, z-order)
    /// </summary>
    public partial class MainWindow
    {
       
        private void DeleteElement_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElements.Count == 0) return;

            var count = _selectedElements.Count;
            var message = count == 1
                ? $"Are you sure you want to delete '{_selectedElements[0].Name}'?"
                : $"Are you sure you want to delete {count} elements?";

            var result = MessageBox.Show(message, "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Delete all selected elements
                foreach (var element in _selectedElements.ToList())
                {
                    var command = new DeleteElementCommand(
                        element,
                        element.Parent,
                        RootElements,
                        RenderElement,
                        RemoveElementVisual);
                    _undoRedoManager.Execute(command);
                }

                ClearSelection();
                UpdatePropertyPanel();
            }
        }

        private void DuplicateElement_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElements.Count == 0) return;

            // Duplicate all selected elements
            var newElements = new List<UIElementViewModel>();
            foreach (var element in _selectedElements)
            {
                // Use Clone to copy all properties - same position (visible in hierarchy)
                var duplicate = element.Clone(GenerateElementName(element.ElementType));

                // Determine parent - same parent as selected, or first root
                var parent = element.Parent ?? (RootElements.Count > 0 ? RootElements[0] : null);

                var command = new CreateElementCommand(
                    duplicate,
                    parent,
                    RootElements,
                    RenderElement,
                    RemoveElementVisual,
                    el => { }); // Don't select during loop
                _undoRedoManager.Execute(command);
                newElements.Add(duplicate);
            }

            // Select all duplicated elements
            ClearSelection();
            foreach (var element in newElements)
            {
                element.IsSelected = true;
                _selectedElements.Add(element);
            }
            UpdatePropertyPanel();
        }

        private void CreateParent_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElements.Count == 0) return;

            // IMPORTANT: Only group the "root" elements of the selection - elements that are NOT
            // descendants of other selected elements. Children will move with their parents.
            var elementsToGroup = _selectedRootElements.ToList();
            if (elementsToGroup.Count == 0) return;

            // Calculate bounding box of selected ROOT elements only
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var element in elementsToGroup)
            {
                minX = Math.Min(minX, element.X);
                minY = Math.Min(minY, element.Y);
                maxX = Math.Max(maxX, element.X + element.Width);
                maxY = Math.Max(maxY, element.Y + element.Height);
            }

            // Create new Canvas parent sized to fit all children
            var name = GenerateElementName("Canvas");
            var parentCanvas = new UIElementViewModel(name, "Canvas")
            {
                Id = Guid.NewGuid().ToString(),
                X = minX,
                Y = minY,
                Width = maxX - minX,
                Height = maxY - minY
            };

            // Find common parent of selected elements (use first element's parent)
            var commonParent = elementsToGroup[0].Parent;

            // Create and execute the group command
            var command = new GroupElementsCommand(
                elementsToGroup,
                parentCanvas,
                commonParent,
                RootElements,
                () => _renderService.RenderAllElements(RootElements),
                el => { ClearSelection(); SelectElement(el, false); },
                minX,
                minY);

            _undoRedoManager.Execute(command);
        }

        private void BringToFront_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElements.Count == 0) return;

            var command = new ZOrderCommand(
                _selectedElements.ToList(),
                RootElements,
                bringToFront: true,
                UpdateVisualZOrder);

            _undoRedoManager.Execute(command);
        }

        /// <summary>
        /// Updates the visual Z-order for an element on the canvas
        /// </summary>
        private void UpdateVisualZOrder(UIElementViewModel element)
        {
            var visual = _renderService.GetVisual(element.Id) as UIElementVisual;
            if (visual != null)
            {
                // Re-render all to ensure correct Z-order
                _renderService.RenderAllElements(RootElements);
            }
        }

        private void SendToBack_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElements.Count == 0) return;

            var command = new ZOrderCommand(
                _selectedElements.ToList(),
                RootElements,
                bringToFront: false,
                UpdateVisualZOrder);

            _undoRedoManager.Execute(command);
        }

        /// <summary>
        /// Recalculates ZIndex for all children of a parent based on their position in the Children collection.
        /// This ensures draw order matches hierarchy order.
        /// </summary>
        private static void RecalculateSiblingZIndices(UIElementViewModel parent)
        {
            for (int i = 0; i < parent.Children.Count; i++)
            {
                parent.Children[i].ZIndex = i;
            }
        }

        private void ResetPosition_Click(object sender, RoutedEventArgs e)
        {
            var command = _alignmentService.ResetPosition(_selectedElements.ToList());
            if (command != null)
            {
                _undoRedoManager.Execute(command);
                UpdatePropertyPanel();
            }
        }

        private void ResetSize_Click(object sender, RoutedEventArgs e)
        {
            var command = _alignmentService.ResetSize(_selectedElements.ToList());
            if (command != null)
            {
                _undoRedoManager.Execute(command);
                UpdatePropertyPanel();
            }
        }

    }
}

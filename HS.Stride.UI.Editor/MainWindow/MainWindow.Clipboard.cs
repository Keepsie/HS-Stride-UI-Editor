// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using HS.Stride.UI.Editor.ViewModels;
using HS.Stride.UI.Editor.Models.Commands;

namespace HS.Stride.UI.Editor
{
    /// <summary>
    /// MainWindow partial class - Cut/Copy/Paste and Nudge operations
    /// </summary>
    public partial class MainWindow
    {
        private List<UIElementViewModel> _clipboardElements = new();

        private void CutElement()
        {
            if (_selectedElements.Count == 0) return;

            CopyElement();
            DeleteElement_Click(this, new RoutedEventArgs());
        }

        private void CopyElement()
        {
            if (_selectedElements.Count == 0) return;
            _clipboardElements = _selectedElements.ToList();
        }

        private void PasteElement()
        {
            if (_clipboardElements.Count == 0) return;

            var newElements = new List<UIElementViewModel>();
            foreach (var clipboardElement in _clipboardElements)
            {
                var pastedElement = new UIElementViewModel(GenerateElementName(clipboardElement.ElementType), clipboardElement.ElementType)
                {
                    X = clipboardElement.X + 20,
                    Y = clipboardElement.Y + 20,
                    Width = clipboardElement.Width,
                    Height = clipboardElement.Height
                };

                var parent = RootElements.Count > 0 ? RootElements[0] : null;
                var command = new CreateElementCommand(
                    pastedElement,
                    parent,
                    RootElements,
                    RenderElement,
                    RemoveElementVisual,
                    el => { }); // Don't select during loop
                _undoRedoManager.Execute(command);
                newElements.Add(pastedElement);
            }

            // Select all pasted elements
            ClearSelection();
            foreach (var element in newElements)
            {
                element.IsSelected = true;
                _selectedElements.Add(element);
            }
            UpdatePropertyPanel();
        }

        private void NudgeElement(double deltaX, double deltaY)
        {
            if (_selectedElements.Count == 0) return;

            foreach (var element in _selectedElements)
            {
                // Skip button content - position is controlled by alignment only
                if (element.IsButtonContent)
                    continue;

                var oldX = element.X;
                var oldY = element.Y;

                // Calculate new position
                var newX = Math.Round(oldX + deltaX);
                var newY = Math.Round(oldY + deltaY);

                // Only clamp to artboard bounds for root elements
                // Child elements can be positioned outside their parent bounds intentionally
                bool isRootElement = element.Parent == null || element.Parent.IsSystemElement;
                if (isRootElement)
                {
                    newX = Math.Max(0, Math.Min(newX, _designWidth - element.Width));
                    newY = Math.Max(0, Math.Min(newY, _designHeight - element.Height));
                }

                var command = new MoveElementCommand(
                    element,
                    oldX, oldY,
                    newX, newY);
                _undoRedoManager.Execute(command);
            }

            UpdatePropertyPanel();
            UpdateGroupSelectionOverlay();
        }

       
    }
}

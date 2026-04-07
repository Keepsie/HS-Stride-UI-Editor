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
        private sealed class ClipboardEntry
        {
            public UIElementViewModel Snapshot { get; }
            public string? OriginalParentId { get; }
            public bool PreferRootParent { get; }

            public ClipboardEntry(UIElementViewModel snapshot, string? originalParentId, bool preferRootParent)
            {
                Snapshot = snapshot;
                OriginalParentId = originalParentId;
                PreferRootParent = preferRootParent;
            }
        }

        private List<ClipboardEntry> _clipboardElements = new();

        private void CutElement()
        {
            if (_selectedElements.Count == 0) return;

            CopyElement();
            DeleteElement_Click(this, new RoutedEventArgs());
        }

        private void CopyElement()
        {
            if (_selectedElements.Count == 0) return;

            // Snapshot: deep-clone selected root elements so the clipboard
            // is immune to later edits on the originals.
            _clipboardElements = new List<ClipboardEntry>();
            foreach (var el in _selectedRootElements.Count > 0 ? _selectedRootElements : _selectedElements)
            {
                var snapshot = DeepCloneElement(el);
                var parent = el.Parent;
                _clipboardElements.Add(new ClipboardEntry(
                    snapshot,
                    parent?.Id,
                    parent == null || parent.IsSystemElement));
            }
        }

        /// <summary>
        /// Recursively deep-clones an element and all its children.
        /// Each clone gets a fresh ID and a generated name.
        /// </summary>
        private UIElementViewModel DeepCloneElement(UIElementViewModel source)
        {
            var clone = source.Clone(GenerateElementName(source.ElementType));

            foreach (var child in source.Children)
            {
                var childClone = DeepCloneElement(child);
                clone.Children.Add(childClone);
            }

            return clone;
        }

        private UIElementViewModel? FindElementById(string id)
        {
            foreach (var root in RootElements)
            {
                var match = FindElementByIdRecursive(root, id);
                if (match != null) return match;
            }
            return null;
        }

        private UIElementViewModel? FindElementByIdRecursive(UIElementViewModel current, string id)
        {
            if (current.Id == id)
                return current;

            foreach (var child in current.Children)
            {
                var match = FindElementByIdRecursive(child, id);
                if (match != null) return match;
            }

            return null;
        }

        private void PasteElement()
        {
            if (_clipboardElements.Count == 0) return;

            var newElements = new List<UIElementViewModel>();
            foreach (var clipboardEntry in _clipboardElements)
            {
                // Deep-clone again so the same clipboard can be pasted multiple times
                var pastedElement = DeepCloneElement(clipboardEntry.Snapshot);
                pastedElement.X += 20;
                pastedElement.Y += 20;

                UIElementViewModel? parent = null;
                if (!clipboardEntry.PreferRootParent && !string.IsNullOrEmpty(clipboardEntry.OriginalParentId))
                {
                    parent = FindElementById(clipboardEntry.OriginalParentId);
                }

                parent ??= RootElements.Count > 0 ? RootElements[0] : null;
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

            // Select all pasted elements and sync to hierarchy
            ClearSelection();
            foreach (var element in newElements)
            {
                AddToSelection(element);
            }
            UpdatePropertyPanel();
            SyncSelectionToHierarchy();
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

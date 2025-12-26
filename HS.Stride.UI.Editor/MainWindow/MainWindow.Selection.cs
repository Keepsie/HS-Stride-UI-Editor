// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor
{
    /// <summary>
    /// MainWindow partial class - Selection management
    /// </summary>
    public partial class MainWindow
    {
        private void SelectElement(UIElementViewModel element, bool addToSelection = false)
        {
            if (addToSelection)
            {
                // Ctrl+Click: Toggle selection
                if (_selectedElements.Contains(element))
                {
                    RemoveFromSelection(element);
                }
                else
                {
                    AddToSelection(element);
                }
            }
            else
            {
                // Normal click: If element is already selected and we have multiple selections,
                // keep the multi-selection intact (for multi-drag support)
                if (_selectedElements.Contains(element) && _selectedElements.Count > 1)
                {
                    // Element already selected in multi-selection, don't change selection
                    return;
                }
                else
                {
                    // Clear and select only this
                    ClearSelection();
                    AddToSelection(element);
                }
            }

            // Update property panel
            UpdatePropertyPanel();

            // Sync selection to hierarchy tree (single selection only)
            SyncSelectionToHierarchy();
        }

        /// <summary>
        /// Add element to selection, maintaining SelectedRootElements (Stride pattern)
        /// </summary>
        private void AddToSelection(UIElementViewModel element)
        {
            if (_selectedElements.Contains(element)) return;

            element.IsSelected = true;
            _selectedElements.Add(element);

            // Check if any of this element's ancestors is already selected
            var parent = element.Parent;
            while (parent != null && !parent.IsSystemElement)
            {
                if (_selectedElements.Contains(parent))
                {
                    // Parent is selected, so this element is NOT a root selection
                    UpdateAlignmentButtonStates();
                    UpdateGroupSelectionOverlay();
                    return;
                }
                parent = parent.Parent;
            }

            // No ancestor is selected, so this is a root selection
            _selectedRootElements.Add(element);

            // Remove any descendants from root selection (they now have a selected ancestor)
            RemoveDescendantsFromRootSelection(element);
            UpdateAlignmentButtonStates();
            UpdateGroupSelectionOverlay();
        }

        /// <summary>
        /// Remove element from selection
        /// </summary>
        private void RemoveFromSelection(UIElementViewModel element)
        {
            if (!_selectedElements.Contains(element)) return;

            element.IsSelected = false;
            _selectedElements.Remove(element);
            _selectedRootElements.Remove(element);

            // Check if any of its selected descendants should now become root selections
            foreach (var child in element.Children)
            {
                PromoteDescendantsToRootIfSelected(child);
            }
            UpdateAlignmentButtonStates();
            UpdateGroupSelectionOverlay();
        }

        /// <summary>
        /// Remove all descendants of element from _selectedRootElements
        /// </summary>
        private void RemoveDescendantsFromRootSelection(UIElementViewModel element)
        {
            foreach (var child in element.Children)
            {
                _selectedRootElements.Remove(child);
                RemoveDescendantsFromRootSelection(child);
            }
        }

        /// <summary>
        /// If a descendant is selected but not in root selection, add it
        /// </summary>
        private void PromoteDescendantsToRootIfSelected(UIElementViewModel element)
        {
            if (_selectedElements.Contains(element) && !_selectedRootElements.Contains(element))
            {
                // Check if any ancestor is still selected
                var parent = element.Parent;
                while (parent != null && !parent.IsSystemElement)
                {
                    if (_selectedElements.Contains(parent))
                        return; // Still has selected ancestor
                    parent = parent.Parent;
                }
                _selectedRootElements.Add(element);
            }

            foreach (var child in element.Children)
            {
                PromoteDescendantsToRootIfSelected(child);
            }
        }

        /// <summary>
        /// Syncs the current selection to the hierarchy tree view
        /// </summary>
        private void SyncSelectionToHierarchy()
        {
            if (_selectedElements.Count != 1) return;

            // Use flag to prevent recursive events
            _isUpdatingTreeViewSelection = true;
            try
            {
                SelectTreeViewItem(VisualTreeView, _selectedElements[0]);
            }
            finally
            {
                _isUpdatingTreeViewSelection = false;
            }
        }

        /// <summary>
        /// Recursively find and select a TreeViewItem by its data context
        /// </summary>
        private bool SelectTreeViewItem(ItemsControl parent, UIElementViewModel target)
        {
            if (parent == null) return false;

            foreach (var item in parent.Items)
            {
                var container = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (container == null)
                {
                    // Container not generated yet - expand parent to generate it
                    if (parent is TreeViewItem tvi)
                    {
                        tvi.IsExpanded = true;
                        tvi.UpdateLayout();
                        container = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                    }
                }

                if (container != null)
                {
                    if (item == target)
                    {
                        container.IsSelected = true;
                        container.BringIntoView();
                        return true;
                    }

                    // Recursively check children
                    if (SelectTreeViewItem(container, target))
                    {
                        container.IsExpanded = true;
                        return true;
                    }
                }
            }

            return false;
        }

        private void ClearSelection()
        {
            foreach (var element in _selectedElements)
            {
                element.IsSelected = false;
            }
            _selectedElements.Clear();
            _selectedRootElements.Clear();
            UpdateAlignmentButtonStates();
            UpdateGroupSelectionOverlay();

            // Clear TreeView selection
            _isUpdatingTreeViewSelection = true;
            try
            {
                // Find and deselect any selected TreeViewItem
                var selectedItem = VisualTreeView.SelectedItem;
                if (selectedItem != null)
                {
                    var container = FindTreeViewItemForElement(VisualTreeView, selectedItem);
                    if (container != null)
                    {
                        container.IsSelected = false;
                    }
                }
            }
            finally
            {
                _isUpdatingTreeViewSelection = false;
            }
        }

        /// <summary>
        /// Find TreeViewItem container for a given element
        /// </summary>
        private TreeViewItem? FindTreeViewItemForElement(ItemsControl parent, object element)
        {
            if (parent == null) return null;

            var container = parent.ItemContainerGenerator.ContainerFromItem(element) as TreeViewItem;
            if (container != null) return container;

            // Search children
            foreach (var item in parent.Items)
            {
                var childContainer = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (childContainer != null)
                {
                    var result = FindTreeViewItemForElement(childContainer, element);
                    if (result != null) return result;
                }
            }

            return null;
        }

        private void VisualTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Ignore selection changes while we're programmatically updating
            if (_isUpdatingTreeViewSelection) return;

            if (e.NewValue is UIElementViewModel element)
            {
                SelectElement(element);
            }
        }

        /// <summary>
        /// Get the primary selected element (first in list)
        /// </summary>
        private UIElementViewModel? PrimarySelection => _selectedElements.Count > 0 ? _selectedElements[0] : null;

        /// <summary>
        /// Update alignment/distribute button enabled states based on selection count
        /// </summary>
        private void UpdateAlignmentButtonStates()
        {
            var count = _selectedElements.Count;

            // Alignment enabled for:
            // - 2+ elements (multi-element alignment)
            // - 1 element with a non-system parent (parent alignment)
            var multiAlignEnabled = count >= 2;
            var parentAlignEnabled = count == 1 &&
                _selectedElements[0].Parent != null &&
                !_selectedElements[0].Parent.IsSystemElement;
            var alignEnabled = multiAlignEnabled || parentAlignEnabled;

            AlignLeftButton.IsEnabled = alignEnabled;
            AlignCenterHButton.IsEnabled = alignEnabled;
            AlignRightButton.IsEnabled = alignEnabled;
            AlignTopButton.IsEnabled = alignEnabled;
            AlignCenterVButton.IsEnabled = alignEnabled;
            AlignBottomButton.IsEnabled = alignEnabled;

            // Update label to show current alignment mode
            if (parentAlignEnabled)
                AlignmentModeLabel.Text = "Align to Parent:";
            else if (multiAlignEnabled)
                AlignmentModeLabel.Text = "Multi-Element Align:";
            else
                AlignmentModeLabel.Text = "Align:";

            // Distribute requires 3+ elements
            var distributeEnabled = count >= 3;
            DistributeHButton.IsEnabled = distributeEnabled;
            DistributeVButton.IsEnabled = distributeEnabled;
        }

    }
}

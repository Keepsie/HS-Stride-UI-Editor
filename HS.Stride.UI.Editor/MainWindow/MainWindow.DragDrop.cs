// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HS.Stride.UI.Editor.Core.Abstractions;
using HS.Stride.UI.Editor.Core.Services;
using HS.Stride.UI.Editor.Models;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor
{
    /// <summary>
    /// MainWindow partial class - Drag and drop handlers (canvas, tree view, library)
    /// </summary>
    public partial class MainWindow
    {

        // Track for auto-expand during drag
        private TreeViewItem? _lastHoveredItem;
        private DateTime _hoverStartTime;
        private const double AUTO_EXPAND_DELAY_MS = 500;

        // Drop indicator tracking
        private enum DropPosition { None, Before, After, Inside }
        private DropPosition _currentDropPosition = DropPosition.None;
        private TreeViewItem? _currentDropTarget;

        private void EditorCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Deselect all elements when clicking outside the artboard (on the outer canvas area)
            if (e.OriginalSource == EditorCanvas)
            {
                ClearSelection();
                UpdatePropertyPanel();
                e.Handled = true;
            }
        }

        private void RootArtboard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Deselect all elements when clicking on empty canvas (Border background)
            if (e.OriginalSource == sender || e.OriginalSource is Border)
            {
                ClearSelection();
                UpdatePropertyPanel();
                e.Handled = true;
            }
        }

        private void ArtboardCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only start selection box if clicking directly on the canvas, not on a child element
            if (e.OriginalSource == ArtboardCanvas)
            {
                // Start selection box
                _isDrawingSelectionBox = true;
                _selectionBoxStart = e.GetPosition(ArtboardCanvas);

                // Clear selection unless Ctrl is held (additive selection)
                bool ctrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
                if (!ctrlPressed)
                {
                    ClearSelection();
                    UpdatePropertyPanel();
                }

                // Initialize selection box
                Canvas.SetLeft(SelectionBox, _selectionBoxStart.X);
                Canvas.SetTop(SelectionBox, _selectionBoxStart.Y);
                SelectionBox.Width = 0;
                SelectionBox.Height = 0;
                SelectionBox.Visibility = Visibility.Visible;

                ArtboardCanvas.CaptureMouse();
                e.Handled = true;
            }
        }

        private void ArtboardCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawingSelectionBox && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPos = e.GetPosition(ArtboardCanvas);

                // Calculate selection box bounds (handle negative drag direction)
                double x = Math.Min(_selectionBoxStart.X, currentPos.X);
                double y = Math.Min(_selectionBoxStart.Y, currentPos.Y);
                double width = Math.Abs(currentPos.X - _selectionBoxStart.X);
                double height = Math.Abs(currentPos.Y - _selectionBoxStart.Y);

                Canvas.SetLeft(SelectionBox, x);
                Canvas.SetTop(SelectionBox, y);
                SelectionBox.Width = width;
                SelectionBox.Height = height;
            }
        }

        private void ArtboardCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawingSelectionBox)
            {
                _isDrawingSelectionBox = false;
                ArtboardCanvas.ReleaseMouseCapture();

                // Hide selection box
                SelectionBox.Visibility = Visibility.Collapsed;

                // Get selection box bounds
                double boxX = Canvas.GetLeft(SelectionBox);
                double boxY = Canvas.GetTop(SelectionBox);
                double boxWidth = SelectionBox.Width;
                double boxHeight = SelectionBox.Height;

                // Only select if box has some size (not just a click)
                if (boxWidth > 5 || boxHeight > 5)
                {
                    var selectionRect = new Rect(boxX, boxY, boxWidth, boxHeight);
                    SelectElementsInRect(selectionRect);
                }

                e.Handled = true;
            }
        }

        private void SelectElementsInRect(Rect selectionRect)
        {
            // Find all elements that intersect with the selection rectangle
            var elementsToSelect = new List<UIElementViewModel>();

            foreach (var element in GetAllElements())
            {
                if (element.IsSystemElement) continue;
                if (element.IsLocked) continue; // Skip locked elements

                // Get element bounds in artboard coordinates
                var elementRect = GetElementWorldBounds(element);

                // Check intersection
                if (selectionRect.IntersectsWith(elementRect))
                {
                    elementsToSelect.Add(element);
                }
            }

            // Select all intersecting elements
            bool ctrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            foreach (var element in elementsToSelect)
            {
                SelectElement(element, addToSelection: true);
            }

            UpdatePropertyPanel();
        }

        private Rect GetElementWorldBounds(UIElementViewModel element)
        {
            // Calculate world position by adding parent positions
            double worldX = element.X;
            double worldY = element.Y;

            var parent = element.Parent;
            while (parent != null && !parent.IsSystemElement)
            {
                worldX += parent.X;
                worldY += parent.Y;
                parent = parent.Parent;
            }

            return new Rect(worldX, worldY, element.Width, element.Height);
        }

        private IEnumerable<UIElementViewModel> GetAllElements()
        {
            // Recursively get all elements from root
            foreach (var root in RootElements)
            {
                foreach (var element in GetElementAndChildren(root))
                {
                    yield return element;
                }
            }
        }

        private IEnumerable<UIElementViewModel> GetElementAndChildren(UIElementViewModel element)
        {
            yield return element;
            foreach (var child in element.Children)
            {
                foreach (var descendant in GetElementAndChildren(child))
                {
                    yield return descendant;
                }
            }
        }

        private void VisualTreeView_DragOver(object sender, DragEventArgs e)
        {
            // Default to no drop allowed
            e.Effects = DragDropEffects.None;

            // Block when no document is loaded
            if (!_isDocumentLoaded)
            {
                HideDropIndicator();
                e.Handled = true;
                return;
            }

            // Auto-scroll when dragging near edges
            AutoScrollHierarchy(e.GetPosition(HierarchyScrollViewer));

            // Find the target element for hover expansion
            var dropTargetItem = FindTreeViewItemFromPoint(e.GetPosition(VisualTreeView));

            // Auto-expand container when hovering over it during drag
            if (dropTargetItem != null && dropTargetItem != _lastHoveredItem)
            {
                _lastHoveredItem = dropTargetItem;
                _hoverStartTime = DateTime.Now;
            }
            else if (dropTargetItem != null && dropTargetItem == _lastHoveredItem && !dropTargetItem.IsExpanded)
            {
                // Auto-expand after hovering for a short time
                if ((DateTime.Now - _hoverStartTime).TotalMilliseconds > AUTO_EXPAND_DELAY_MS)
                {
                    dropTargetItem.IsExpanded = true;
                }
            }

            // Calculate drop position for indicator
            DropPosition dropPos = DropPosition.Inside;
            if (dropTargetItem != null)
            {
                var relativePos = e.GetPosition(dropTargetItem);
                dropPos = CalculateDropPosition(relativePos.Y, dropTargetItem.ActualHeight);
            }

            if (e.Data.GetDataPresent(typeof(UIElementFactory)) ||
                e.Data.GetDataPresent(typeof(AssetItem)))
            {
                // Dropping new elements from library or assets - always allow on tree
                e.Effects = DragDropEffects.Copy;
                UpdateDropIndicator(dropTargetItem, dropPos);
            }
            else if (e.Data.GetDataPresent(typeof(List<UIElementViewModel>)))
            {
                // Reparenting existing elements within hierarchy (multi-select)
                var draggedElements = e.Data.GetData(typeof(List<UIElementViewModel>)) as List<UIElementViewModel>;
                if (draggedElements != null && draggedElements.Count > 0)
                {
                    UIElementViewModel? targetElement = dropTargetItem?.DataContext as UIElementViewModel;

                    // Default to first root if no specific target (dropping on empty area)
                    if (targetElement == null && RootElements.Count > 0)
                    {
                        targetElement = RootElements[0];
                        dropPos = DropPosition.Inside;
                    }

                    // Validate reparenting for all dragged elements
                    bool allValid = targetElement != null &&
                                    draggedElements.All(el => _dragDropService.IsValidReparent(el, targetElement));
                    if (allValid)
                    {
                        e.Effects = DragDropEffects.Move;
                        UpdateDropIndicator(dropTargetItem, dropPos);
                    }
                    else
                    {
                        HideDropIndicator();
                    }
                }
                else
                {
                    HideDropIndicator();
                }
            }
            else
            {
                HideDropIndicator();
            }

            e.Handled = true;
        }

        /// <summary>
        /// Auto-scroll the hierarchy ScrollViewer when dragging near edges
        /// </summary>
        private void AutoScrollHierarchy(Point mousePosition)
        {
            const double scrollZone = 30; // Pixels from edge to trigger scroll
            const double scrollSpeed = 10; // Pixels to scroll per event

            double scrollViewerHeight = HierarchyScrollViewer.ActualHeight;

            if (mousePosition.Y < scrollZone)
            {
                // Near top - scroll up
                double scrollAmount = scrollSpeed * (1 - mousePosition.Y / scrollZone);
                HierarchyScrollViewer.ScrollToVerticalOffset(HierarchyScrollViewer.VerticalOffset - scrollAmount);
            }
            else if (mousePosition.Y > scrollViewerHeight - scrollZone)
            {
                // Near bottom - scroll down
                double distanceFromBottom = scrollViewerHeight - mousePosition.Y;
                double scrollAmount = scrollSpeed * (1 - distanceFromBottom / scrollZone);
                HierarchyScrollViewer.ScrollToVerticalOffset(HierarchyScrollViewer.VerticalOffset + scrollAmount);
            }
        }

        /// <summary>
        /// Update the visual drop indicator to show where the drop will occur
        /// </summary>
        private void UpdateDropIndicator(TreeViewItem? targetItem, DropPosition position)
        {
            _currentDropTarget = targetItem;
            _currentDropPosition = position;

            if (targetItem == null || position == DropPosition.None)
            {
                HideDropIndicator();
                return;
            }

            // Show the indicator canvas
            HierarchyDropIndicator.Visibility = Visibility.Visible;

            // Get the position of the target item relative to the hierarchy container
            var containerGrid = HierarchyDropIndicator.Parent as Grid;
            if (containerGrid == null) return;

            var itemBounds = targetItem.TransformToAncestor(containerGrid).TransformBounds(
                new Rect(0, 0, targetItem.ActualWidth, targetItem.ActualHeight));

            // Calculate indicator position based on drop position
            double lineY;
            double lineX = itemBounds.Left + 20; // Indent to align with text
            double lineWidth = Math.Max(150, itemBounds.Width - 20);

            if (position == DropPosition.Before)
            {
                lineY = itemBounds.Top - 1;
                DropIndicatorLine.Visibility = Visibility.Visible;
                DropIndicatorArrow.Visibility = Visibility.Visible;
            }
            else if (position == DropPosition.After)
            {
                lineY = itemBounds.Bottom - 1;
                DropIndicatorLine.Visibility = Visibility.Visible;
                DropIndicatorArrow.Visibility = Visibility.Visible;
            }
            else // Inside
            {
                // For "inside", highlight the target item with a border effect
                lineY = itemBounds.Top + 2;
                lineX = itemBounds.Left + 4;
                lineWidth = itemBounds.Width - 8;
                DropIndicatorLine.Height = itemBounds.Height - 4;
                DropIndicatorLine.Fill = new SolidColorBrush(Color.FromArgb(60, 10, 132, 255));
                DropIndicatorLine.Visibility = Visibility.Visible;
                DropIndicatorArrow.Visibility = Visibility.Collapsed;

                // Position and show
                Canvas.SetLeft(DropIndicatorLine, lineX);
                Canvas.SetTop(DropIndicatorLine, lineY);
                DropIndicatorLine.Width = lineWidth;
                return;
            }

            // Position the line indicator
            DropIndicatorLine.Height = 2;
            DropIndicatorLine.Fill = new SolidColorBrush(Color.FromRgb(10, 132, 255));
            Canvas.SetLeft(DropIndicatorLine, lineX);
            Canvas.SetTop(DropIndicatorLine, lineY);
            DropIndicatorLine.Width = lineWidth;

            // Position the arrow
            Canvas.SetLeft(DropIndicatorArrow, lineX - 10);
            Canvas.SetTop(DropIndicatorArrow, lineY - 3);
        }

        /// <summary>
        /// Hide the drop indicator
        /// </summary>
        private void HideDropIndicator()
        {
            HierarchyDropIndicator.Visibility = Visibility.Collapsed;
            DropIndicatorLine.Visibility = Visibility.Collapsed;
            DropIndicatorArrow.Visibility = Visibility.Collapsed;
            _currentDropPosition = DropPosition.None;
            _currentDropTarget = null;
        }

        /// <summary>
        /// Calculate drop position based on mouse Y within the target item
        /// </summary>
        private DropPosition CalculateDropPosition(double relativeY, double itemHeight)
        {
            // Match the 20% threshold used in DragDropService.CalculateReparentDropInfo
            double edgeThreshold = itemHeight * 0.20;

            if (relativeY < edgeThreshold)
                return DropPosition.Before;
            else if (relativeY > itemHeight - edgeThreshold)
                return DropPosition.After;
            else
                return DropPosition.Inside;
        }

        private void VisualTreeView_Drop(object sender, DragEventArgs e)
        {
            // Reset hover tracking and hide drop indicator
            _lastHoveredItem = null;
            HideDropIndicator();

            // Block drops when no document is loaded
            if (!_isDocumentLoaded) return;

            // Find the TreeViewItem that was dropped on
            var dropTargetItem = FindTreeViewItemFromPoint(e.GetPosition(VisualTreeView));
            UIElementViewModel? targetElement = dropTargetItem?.DataContext as UIElementViewModel;

            // Default to root if no specific target
            bool droppedOnRoot = false;
            if (targetElement == null && RootElements.Count > 0)
            {
                targetElement = RootElements[0];
                droppedOnRoot = true;
            }

            if (targetElement == null) return;

            // Handle dropping elements from the hierarchy (reparenting) - supports multi-select
            if (e.Data.GetDataPresent(typeof(List<UIElementViewModel>)))
            {
                var draggedElements = e.Data.GetData(typeof(List<UIElementViewModel>)) as List<UIElementViewModel>;
                if (draggedElements == null || draggedElements.Count == 0) return;

                // Calculate drop info using first element (all go to same parent)
                var firstElement = draggedElements[0];
                var wpfRelativePos = dropTargetItem != null ? e.GetPosition(dropTargetItem) : new Point(0, 0);
                var relativePos = Point2D.FromWpf(wpfRelativePos);
                double targetHeight = dropTargetItem?.ActualHeight ?? 0;

                var dropInfo = _dragDropService.CalculateReparentDropInfo(
                    firstElement,
                    targetElement,
                    relativePos,
                    targetHeight,
                    droppedOnRoot);

                if (!dropInfo.IsValid || dropInfo.NewParent == null)
                    return;

                // Reparent all dragged elements
                int insertIndex = dropInfo.InsertIndex;
                foreach (var draggedElement in draggedElements)
                {
                    // Skip if this element can't be reparented to target
                    if (!_dragDropService.IsValidReparent(draggedElement, dropInfo.NewParent))
                        continue;

                    // Create reparent command - pass render callback to actually update visuals
                    var command = _dragDropService.CreateReparentCommand(
                        draggedElement,
                        dropInfo.NewParent,
                        insertIndex, // Use calculated index for proper before/after placement
                        RootElements,
                        () => _renderService.RenderAllElements(RootElements), // Re-render after reparenting!
                        () => TransformCoordinatesForNewParent(draggedElement, draggedElement.Parent, dropInfo.NewParent));

                    _undoRedoManager.Execute(command);

                    // For multi-select, increment index so next element goes after this one
                    if (insertIndex >= 0) insertIndex++;
                }

                // Keep selection on dragged elements
                ClearSelection();
                foreach (var el in draggedElements)
                {
                    AddToSelection(el);
                }

                e.Handled = true;
                return;
            }

            // Handle dropping assets from Project Content
            if (e.Data.GetDataPresent(typeof(AssetItem)))
            {
                var asset = (AssetItem)e.Data.GetData(typeof(AssetItem));

                // Get scaled image dimensions (maintains aspect ratio, max 200px)
                double? width = null;
                double? height = null;
                if (asset.Type == "Texture" || asset.Type == "SpriteSheet")
                {
                    var dimensions = _assetService.GetScaledImageDimensions(asset.Name);
                    if (dimensions.HasValue)
                    {
                        width = dimensions.Value.Width;
                        height = dimensions.Value.Height;
                    }
                }

                var command = _dragDropService.CreateElementFromAssetOnTree(
                    asset,
                    targetElement,
                    RootElements,
                    RenderElement,
                    RemoveElementVisual,
                    el => SelectElement(el),
                    width,
                    height);

                if (command != null)
                {
                    _undoRedoManager.Execute(command);
                }

                e.Handled = true;
                return;
            }

            // Handle dropping from UI Library
            if (e.Data.GetDataPresent(typeof(UIElementFactory)))
            {
                var factory = (UIElementFactory)e.Data.GetData(typeof(UIElementFactory));
                var command = _dragDropService.CreateElementFromFactoryOnTree(
                    factory,
                    targetElement,
                    RootElements,
                    RenderElement,
                    RemoveElementVisual,
                    el => SelectElement(el));

                _undoRedoManager.Execute(command);

                e.Handled = true;
            }
        }


        private void VisualTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Don't interfere with scrollbar
            var element = e.OriginalSource as DependencyObject;
            if (element != null && IsScrollBarOrThumb(element))
            {
                return;
            }

            // Find the TreeViewItem that was clicked
            var treeViewItem = FindAncestor<TreeViewItem>(element);
            if (treeViewItem != null && treeViewItem.DataContext is UIElementViewModel clickedElement)
            {
                bool ctrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) != 0;

                // Handle multi-select with Ctrl
                if (ctrlPressed)
                {
                    // Ctrl+Click: Toggle selection without changing TreeView's native selection
                    _isUpdatingTreeViewSelection = true;
                    try
                    {
                        SelectElement(clickedElement, addToSelection: true);
                    }
                    finally
                    {
                        _isUpdatingTreeViewSelection = false;
                    }
                    e.Handled = true; // Prevent TreeView from handling this click
                    return;
                }

                // Record start position for drag threshold
                if (sender is TreeView treeView)
                {
                    _treeViewDragStartPoint = e.GetPosition(treeView);
                    _isTreeViewDragging = false;
                }
            }
        }

        /// <summary>
        /// Find ancestor of specified type in visual tree
        /// </summary>
        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T result)
                    return result;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void VisualTreeView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Reset drag state
            _isTreeViewDragging = false;
        }

        private void VisualTreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is TreeView treeView)
            {
                // Don't interfere with scrollbar dragging
                var element = e.OriginalSource as DependencyObject;
                if (element != null && IsScrollBarOrThumb(element))
                {
                    return;
                }

                // Check if we should initiate a drag - need at least one selected element
                if (!_isTreeViewDragging && _selectedElements.Count > 0)
                {
                    var currentPosition = e.GetPosition(treeView);
                    var delta = currentPosition - _treeViewDragStartPoint;

                    // Only start drag if mouse has moved beyond threshold (prevents accidental drags during selection)
                    if (Math.Abs(delta.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(delta.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        _isTreeViewDragging = true;

                        // Create DataObject with all selected elements (as List for multi-drag)
                        var dataObject = new DataObject(typeof(List<UIElementViewModel>), _selectedElements.ToList());
                        DragDrop.DoDragDrop(treeView, dataObject, DragDropEffects.Move);

                        _isTreeViewDragging = false;
                    }
                }
            }
        }

        private TreeViewItem? FindTreeViewItemFromPoint(Point point)
        {
            var hitTestResult = VisualTreeHelper.HitTest(VisualTreeView, point);
            if (hitTestResult == null) return null;

            var element = hitTestResult.VisualHit as DependencyObject;
            while (element != null && element != VisualTreeView)
            {
                if (element is TreeViewItem tvi)
                    return tvi;
                element = VisualTreeHelper.GetParent(element);
            }
            return null;
        }

        private bool IsScrollBarOrThumb(DependencyObject element)
        {
            // Walk up the visual tree to check if we're over a scrollbar
            while (element != null)
            {
                if (element is System.Windows.Controls.Primitives.ScrollBar ||
                    element is System.Windows.Controls.Primitives.Thumb)
                {
                    return true;
                }
                element = System.Windows.Media.VisualTreeHelper.GetParent(element);
            }
            return false;
        }

        /// <summary>
        /// Calculates element coordinates when reparenting to maintain visual position (Unity-style)
        /// Returns the new local coordinates WITHOUT setting them (to avoid PropertyChanged race conditions)
        /// </summary>
        private (double X, double Y) TransformCoordinatesForNewParent(UIElementViewModel element, UIElementViewModel? oldParent, UIElementViewModel? newParent)
        {
            // Step 1: Calculate current world position (accumulate all parent positions)
            double worldX = element.X;
            double worldY = element.Y;

            var currentParent = oldParent;
            while (currentParent != null && !currentParent.IsSystemElement)
            {
                worldX += currentParent.X;
                worldY += currentParent.Y;
                currentParent = currentParent.Parent;
            }

            // Step 2: Convert world position to new parent's local coordinates
            double localX = worldX;
            double localY = worldY;

            currentParent = newParent;
            while (currentParent != null && !currentParent.IsSystemElement)
            {
                localX -= currentParent.X;
                localY -= currentParent.Y;
                currentParent = currentParent.Parent;
            }

            // Return calculated coordinates WITHOUT setting them
            return (localX, localY);
        }


        private void UILibraryListBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is ListBox listBox)
            {
                // Don't interfere with scrollbar dragging
                var element = e.OriginalSource as DependencyObject;
                if (element != null && IsScrollBarOrThumb(element))
                {
                    return;
                }

                if (listBox.SelectedItem is UIElementFactory factory)
                {
                    // Create drag visual
                    var dragVisual = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(180, 100, 150, 255)),
                        BorderBrush = Brushes.Blue,
                        BorderThickness = new Thickness(2),
                        Padding = new Thickness(8, 4, 8, 4),
                        CornerRadius = new CornerRadius(4),
                        Child = new TextBlock
                        {
                            Text = factory.Name,
                            Foreground = Brushes.White,
                            FontWeight = FontWeights.Bold
                        }
                    };

                    dragVisual.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    dragVisual.Arrange(new Rect(dragVisual.DesiredSize));

                    var dataObject = new DataObject(factory);
                    dataObject.SetData("DragVisual", dragVisual);

                    DragDrop.DoDragDrop(listBox, dataObject, DragDropEffects.Copy);
                }
            }
        }

        private void ProjectContentListView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is ListView listView)
            {
                // Don't interfere with scrollbar dragging
                var element = e.OriginalSource as DependencyObject;
                if (element != null && IsScrollBarOrThumb(element))
                {
                    return;
                }

                if (listView.SelectedItem is AssetItem asset)
                {
                    DragDrop.DoDragDrop(listView, asset, DragDropEffects.Copy);
                }
            }
        }

        private void EditorCanvas_DragOver(object sender, DragEventArgs e)
        {
            // Block when no document is loaded
            if (!_isDocumentLoaded)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            if (e.Data.GetDataPresent(typeof(UIElementFactory)) ||
                e.Data.GetDataPresent(typeof(AssetItem)))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void EditorCanvas_Drop(object sender, DragEventArgs e)
        {
            // Block drops when no document is loaded
            if (!_isDocumentLoaded) return;

            // Get position directly relative to RootArtboard
            var wpfPosition = e.GetPosition(RootArtboard);
            var dropPosition = Point2D.FromWpf(wpfPosition);

            // Clamp to artboard bounds
            dropPosition = _dragDropService.ClampToArtboardBounds(dropPosition, _designWidth, _designHeight);

            var parent = RootElements.Count > 0 ? RootElements[0] : null;

            // Handle dropping assets from Project Content
            if (e.Data.GetDataPresent(typeof(AssetItem)))
            {
                var asset = (AssetItem)e.Data.GetData(typeof(AssetItem));

                // Get scaled image dimensions (maintains aspect ratio, max 200px)
                double? width = null;
                double? height = null;
                if (asset.Type == "Texture" || asset.Type == "SpriteSheet")
                {
                    var dimensions = _assetService.GetScaledImageDimensions(asset.Name);
                    if (dimensions.HasValue)
                    {
                        width = dimensions.Value.Width;
                        height = dimensions.Value.Height;
                    }
                }

                var command = _dragDropService.CreateElementFromAsset(
                    asset,
                    dropPosition,
                    parent,
                    RootElements,
                    RenderElement,
                    RemoveElementVisual,
                    el => SelectElement(el),
                    width,
                    height);

                if (command != null)
                {
                    _undoRedoManager.Execute(command);
                }

                e.Handled = true;
                return;
            }

            // Handle dropping from UI Library
            if (e.Data.GetDataPresent(typeof(UIElementFactory)))
            {
                var factory = (UIElementFactory)e.Data.GetData(typeof(UIElementFactory));
                var command = _dragDropService.CreateElementFromFactory(
                    factory,
                    dropPosition,
                    parent,
                    RootElements,
                    RenderElement,
                    RemoveElementVisual,
                    el => SelectElement(el));

                _undoRedoManager.Execute(command);
                e.Handled = true;
            }
        }

    }
}

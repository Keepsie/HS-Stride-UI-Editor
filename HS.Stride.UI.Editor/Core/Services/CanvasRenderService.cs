// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HS.Stride.UI.Editor.Controls;
using HS.Stride.UI.Editor.Core.Services.Interfaces;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Core.Services
{
    /// <summary>
    /// Manages rendering of UI elements on the canvas, including visual creation, updates, and removal
    /// </summary>
    public class CanvasRenderService : ICanvasRenderService
    {
        private readonly Dictionary<string, UIElementVisual> _visualElements = new();
        private Canvas? _editorCanvas;
        private FrameworkElement? _emptyStateText;

        // Callbacks for MainWindow context
        public Func<string, string, int, Color, ImageSource?>? LoadAssetImageCallback { get; set; }
        public Func<string, System.Windows.Media.FontFamily?>? LoadFontCallback { get; set; }
        public Func<double, double>? SnapToGridCallback { get; set; }
        public Func<(double Width, double Height)>? GetArtboardBoundsCallback { get; set; }
        public Func<double>? GetZoomLevelCallback { get; set; }
        // REMOVED: GetArtboardOffsetCallback - artboard IS the coordinate system now
        public EventHandler<ElementSelectedEventArgs>? ElementSelectedHandler { get; set; }
        public EventHandler<ElementChangedEventArgs>? ElementChangedHandler { get; set; }
        public EventHandler<DragStateEventArgs>? DragStateChangedHandler { get; set; }
        public EventHandler<UIElementViewModel>? AltDragDuplicateHandler { get; set; }
        public ContextMenu? ElementContextMenu { get; set; }
        public Action? UpdateScrollbarVisibilityCallback { get; set; }

        /// <summary>
        /// Callback to handle multi-select drag. Returns true if element should NOT move itself.
        /// Parameters: dragged element ID, deltaX, deltaY
        /// </summary>
        public Func<string, double, double, bool>? HandleMultiSelectDragCallback { get; set; }

        /// <summary>
        /// Callback to apply center snap during drag.
        /// Parameters: elementRect (x, y, width, height)
        /// Returns: (snappedX, snappedY, snappedHorizontally, snappedVertically)
        /// </summary>
        public Func<Rect, (double x, double y, bool snapH, bool snapV)>? CenterSnapCallback { get; set; }

        #region Initialization

        /// <summary>
        /// Initialize the render service with canvas references
        /// </summary>
        public void Initialize(Canvas editorCanvas, FrameworkElement? emptyStateText = null)
        {
            _editorCanvas = editorCanvas;
            _emptyStateText = emptyStateText;
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Render all elements from the root collection
        /// </summary>
        public void RenderAllElements(ObservableCollection<UIElementViewModel> rootElements)
        {
            if (_editorCanvas == null) return;

            // Clear existing UI elements from canvas (but keep the RootArtboard)
            var elementsToRemove = new List<UIElement>();
            foreach (UIElement child in _editorCanvas.Children)
            {
                if (child is UIElementVisual)
                    elementsToRemove.Add(child);
            }
            foreach (var element in elementsToRemove)
            {
                _editorCanvas.Children.Remove(element);
            }

            _visualElements.Clear();

            foreach (var root in rootElements)
            {
                RenderElement(root);
            }

            // Update empty state text visibility
            if (_emptyStateText != null)
            {
                _emptyStateText.Visibility = rootElements.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            // Update scrollbar visibility after rendering
            UpdateScrollbarVisibilityCallback?.Invoke();
        }

        /// <summary>
        /// Render a single element and its children recursively
        /// </summary>
        public void RenderElement(UIElementViewModel element)
        {
            if (_editorCanvas == null) return;

            // Skip rendering system elements (hidden root Grid) but still render their children
            if (!element.IsSystemElement)
            {
                var visual = new UIElementVisual(element);

                // Wire up events
                if (ElementSelectedHandler != null)
                    visual.ElementSelected += ElementSelectedHandler;
                if (ElementChangedHandler != null)
                    visual.ElementChanged += ElementChangedHandler;
                if (DragStateChangedHandler != null)
                    visual.DragStateChanged += DragStateChangedHandler;
                if (AltDragDuplicateHandler != null)
                    visual.AltDragDuplicate += AltDragDuplicateHandler;

                // Set callbacks
                visual.GetAssetImage = LoadAssetImageCallback;
                visual.GetFont = LoadFontCallback;
                visual.SnapValue = SnapToGridCallback;
                visual.GetArtboardBounds = GetArtboardBoundsCallback != null
                    ? new Func<(double, double)>(() => GetArtboardBoundsCallback())
                    : null;
                visual.GetZoomLevel = GetZoomLevelCallback;

                // Wire up multi-select drag callback
                visual.HandleMultiSelectDrag = HandleMultiSelectDragCallback;

                // Wire up center snap callback
                visual.CenterSnap = CenterSnapCallback;

                // Trigger image loading if element has an image source
                if (element.ElementType == "ImageElement" || element.ElementType == "Button")
                {
                    visual.UpdateImageContent();
                }

                // Trigger font loading for text elements
                if (element.ElementType == "TextBlock" || element.ElementType == "EditText")
                {
                    visual.UpdateTextContent();
                }

                // Check if parent is a visual element to support nesting
                UIElementVisual? parentVisual = null;
                if (element.Parent != null && _visualElements.TryGetValue(element.Parent.Id, out var pVisual))
                {
                    parentVisual = pVisual;
                }

                // Set canvas references - this is now the ArtboardCanvas directly
                visual.EditorCanvas = _editorCanvas;

                // REMOVED: GetArtboardOffset - elements are placed directly in artboard coordinates now

                // Set context menu
                if (ElementContextMenu != null)
                    visual.ContextMenu = ElementContextMenu;

                if (parentVisual != null)
                {
                    // Add to parent's child container (Nesting)
                    parentVisual.ChildContainer.Children.Add(visual);

                    // CRITICAL: UpdatePosition() was called in constructor when Parent was null
                    // Now that visual is parented, recalculate position for nested coordinates
                    visual.UpdatePosition();

                    // After adding child, trigger parent's layout (for StackPanel/Grid)
                    parentVisual.LayoutChildren();
                }
                else
                {
                    // Add to root canvas
                    _editorCanvas.Children.Add(visual);
                }

                _visualElements[element.Id] = visual;
            }

            // Recursively render children
            foreach (var child in element.Children)
            {
                RenderElement(child);
            }

            // After all children are rendered, trigger layout for container elements
            if (!element.IsSystemElement && _visualElements.TryGetValue(element.Id, out var containerVisual))
            {
                // Layout children for container types after all children are added
                if (element.ElementType == "StackPanel" || element.ElementType == "Grid")
                {
                    containerVisual.LayoutChildren();
                }
            }
        }

        /// <summary>
        /// Remove an element's visual representation from the canvas
        /// </summary>
        public void RemoveElementVisual(UIElementViewModel element)
        {
            if (_editorCanvas == null) return;

            if (_visualElements.TryGetValue(element.Id, out var visual))
            {
                // Check if nested in a parent visual
                if (visual.Parent is Canvas parentCanvas && parentCanvas != _editorCanvas)
                {
                    parentCanvas.Children.Remove(visual);
                }
                else
                {
                    _editorCanvas.Children.Remove(visual);
                }

                _visualElements.Remove(element.Id);
            }

            // Recursively remove children
            foreach (var child in element.Children)
            {
                RemoveElementVisual(child);
            }
        }

        #endregion

        #region Visual Access

        /// <summary>
        /// Get a visual element by ID (returns object for interface compatibility)
        /// </summary>
        public object? GetVisual(string elementId)
        {
            _visualElements.TryGetValue(elementId, out var visual);
            return visual;
        }

        /// <summary>
        /// Get a visual element by ID (typed version for internal use)
        /// </summary>
        public UIElementVisual? GetVisualTyped(string elementId)
        {
            _visualElements.TryGetValue(elementId, out var visual);
            return visual;
        }

        /// <summary>
        /// Get all visual elements
        /// </summary>
        public IReadOnlyDictionary<string, UIElementVisual> GetAllVisuals()
        {
            return _visualElements;
        }

        /// <summary>
        /// Check if a visual exists for an element
        /// </summary>
        public bool HasVisual(string elementId)
        {
            return _visualElements.ContainsKey(elementId);
        }

        /// <summary>
        /// Sets whether resize handles are visible for an element.
        /// Used to hide individual handles when multiple elements are selected
        /// (the group selection overlay shows handles instead).
        /// </summary>
        public void SetElementHandlesVisible(UIElementViewModel element, bool visible)
        {
            if (_visualElements.TryGetValue(element.Id, out var visual))
            {
                visual.SetHandlesVisible(visible);
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clear all visuals from the canvas
        /// </summary>
        public void ClearAll()
        {
            if (_editorCanvas == null) return;

            var elementsToRemove = new List<UIElement>();
            foreach (UIElement child in _editorCanvas.Children)
            {
                if (child is UIElementVisual)
                    elementsToRemove.Add(child);
            }
            foreach (var element in elementsToRemove)
            {
                _editorCanvas.Children.Remove(element);
            }

            _visualElements.Clear();
        }

        #endregion
    }
}

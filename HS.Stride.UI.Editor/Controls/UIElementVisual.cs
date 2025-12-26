// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using HS.Stride.UI.Editor.Controls.Elements;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Controls
{
    /// <summary>
    /// Event args for element selection
    /// </summary>
    public class ElementSelectedEventArgs : EventArgs
    {
        public UIElementViewModel Element { get; }
        public bool CtrlPressed { get; }
        public bool ShiftPressed { get; }

        public ElementSelectedEventArgs(UIElementViewModel element, bool ctrlPressed, bool shiftPressed)
        {
            Element = element;
            CtrlPressed = ctrlPressed;
            ShiftPressed = shiftPressed;
        }
    }

    /// <summary>
    /// Event args for element change operations (move/resize)
    /// </summary>
    public class ElementChangedEventArgs : EventArgs
    {
        public UIElementViewModel Element { get; }
        public bool IsResize { get; }
        public double OldX { get; }
        public double OldY { get; }
        public double OldWidth { get; }
        public double OldHeight { get; }

        public ElementChangedEventArgs(UIElementViewModel element, bool isResize,
            double oldX, double oldY, double oldWidth, double oldHeight)
        {
            Element = element;
            IsResize = isResize;
            OldX = oldX;
            OldY = oldY;
            OldWidth = oldWidth;
            OldHeight = oldHeight;
        }
    }

    /// <summary>
    /// Event args for drag state changes (for spacing guides)
    /// </summary>
    public class DragStateEventArgs : EventArgs
    {
        public UIElementViewModel Element { get; }
        public bool IsDragging { get; }
        public bool IsResizing { get; }

        public DragStateEventArgs(UIElementViewModel element, bool isDragging, bool isResizing)
        {
            Element = element;
            IsDragging = isDragging;
            IsResizing = isResizing;
        }
    }

    /// <summary>
    /// Visual representation of a UI element on the canvas
    /// </summary>
    public class UIElementVisual : Canvas
    {
        // Element handler for type-specific behavior
        private readonly IElementHandler _handler;

        private Rectangle _mainRect;
        private Border _border;
        public Canvas ChildContainer { get; private set; }
        private Canvas _handleContainer;
        private Rectangle[] _resizeHandles;

        // Lock indicator visual
        private System.Windows.Shapes.Path? _lockIcon;

        private bool _isDragging;
        private bool _isDragStarted; // True only after drag threshold exceeded
        private bool _altDragDuplicateTriggered; // True once Alt+drag duplicate has been created
        private Point _dragStartPoint;
        private Point _dragOffset; // Offset from mouse to element top-left
        private Point _elementStartPos;
        private double _elementStartWidth;
        private double _elementStartHeight;

        // Force hide selection visuals when multi-selected (group overlay is shown instead)
        private bool _forceHideSelectionVisuals = false;

        public UIElementViewModel ViewModel { get; }
        public event EventHandler<ElementSelectedEventArgs>? ElementSelected;
        public event EventHandler<ElementChangedEventArgs>? ElementChanged;
        public event EventHandler<DragStateEventArgs>? DragStateChanged;
        public event EventHandler<UIElementViewModel>? AltDragDuplicate; // Photoshop-style Alt+drag to duplicate

        public Func<string, string, int, Color, ImageSource?>? GetAssetImage { get; set; }
        public Func<string, System.Windows.Media.FontFamily?>? GetFont { get; set; }
        public Func<double, double>? SnapValue { get; set; }

        /// <summary>
        /// Callback to handle multi-select drag. Returns true if this element should NOT move itself
        /// (because a selected parent will move it). Parameters: elementId, deltaX, deltaY
        /// </summary>
        public Func<string, double, double, bool>? HandleMultiSelectDrag { get; set; }
        public Func<(double Width, double Height)>? GetArtboardBounds { get; set; }
        public Func<double>? GetZoomLevel { get; set; }
        // REMOVED: GetArtboardOffset - artboard IS the coordinate system now, no offset needed
        public System.Windows.Controls.Canvas? EditorCanvas { get; set; }

        /// <summary>
        /// Callback for center snap. Returns snapped position and whether snap was applied.
        /// Parameters: elementRect (x, y, width, height)
        /// Returns: (snappedX, snappedY, snappedHorizontally, snappedVertically)
        /// </summary>
        public Func<Rect, (double x, double y, bool snapH, bool snapV)>? CenterSnap { get; set; }

        public UIElementVisual(UIElementViewModel viewModel)
        {
            ViewModel = viewModel;
            _handler = ElementHandlerFactory.GetHandler(viewModel.ElementType);
            InitializeVisual();
            UpdatePosition();
            UpdateSize();
            Panel.SetZIndex(this, ViewModel.DrawLayerNumber);
            this.Opacity = ViewModel.Opacity;
            UpdateVisibility(); // Initialize visibility state
            UpdateSelectionVisual(); // Initialize selection state
            UpdateLockedVisual(); // Initialize locked state (must set IsHitTestVisible on load)

            // Subscribe to property changes
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == null) return;

                // Common properties handled by UIElementVisual
                switch (e.PropertyName)
                {
                    case nameof(UIElementViewModel.X):
                    case nameof(UIElementViewModel.Y):
                        UpdatePosition();
                        return;
                    case nameof(UIElementViewModel.Width):
                    case nameof(UIElementViewModel.Height):
                        UpdateSize();
                        return;
                    case nameof(UIElementViewModel.IsSelected):
                        UpdateSelectionVisual();
                        return;
                    case nameof(UIElementViewModel.DrawLayerNumber):
                        Panel.SetZIndex(this, ViewModel.DrawLayerNumber);
                        return;
                    case nameof(UIElementViewModel.Opacity):
                        this.Opacity = ViewModel.Opacity;
                        return;
                    case nameof(UIElementViewModel.IsLocked):
                        UpdateLockedVisual();
                        return;
                    case nameof(UIElementViewModel.Visibility):
                        UpdateVisibility();
                        return;
                    case nameof(UIElementViewModel.BackgroundColor):
                        _mainRect.Fill = _handler.GetFillBrush(ViewModel);
                        return;
                    case nameof(UIElementViewModel.GridRow):
                    case nameof(UIElementViewModel.GridColumn):
                        // If this element's grid position changed, ask parent to re-layout
                        RequestParentLayout();
                        return;
                }

                // Delegate element-specific property changes to handler
                if (_handler.HandlePropertyChange(this, ViewModel, e.PropertyName))
                {
                    return;
                }
            };
        }

        private void InitializeVisual()
        {
            // Main rectangle (common to all elements)
            _mainRect = new Rectangle
            {
                Fill = _handler.GetFillBrush(ViewModel),
                Stroke = Brushes.Transparent,
                StrokeThickness = 1,
                RadiusX = 2,
                RadiusY = 2
            };

            // Border for selection
            _border = new Border
            {
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(2)
            };

            // Add main rect to canvas
            Children.Add(_mainRect);

            // Delegate element-specific initialization to handler
            _handler.InitializeVisual(this, ViewModel);

            // Container for child elements (nested visuals)
            ChildContainer = new Canvas();
            Children.Add(ChildContainer);

            // Handle container for resize handles
            _handleContainer = new Canvas
            {
                Visibility = Visibility.Collapsed
            };
            Children.Add(_handleContainer);

            // Create resize handles
            CreateResizeHandles();

            // Mouse events
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseMove += OnMouseMove;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;

            // Context menu will be set by parent window
        }

        private void CreateResizeHandles()
        {
            _resizeHandles = new Rectangle[8]; // 8 handles: 4 corners + 4 edges

            var cursors = new[] {
                Cursors.SizeNWSE, // Top-left
                Cursors.SizeNESW, // Top-right
                Cursors.SizeNESW, // Bottom-left
                Cursors.SizeNWSE, // Bottom-right
                Cursors.SizeNS,   // Top
                Cursors.SizeNS,   // Bottom
                Cursors.SizeWE,   // Left
                Cursors.SizeWE    // Right
            };

            for (int i = 0; i < 8; i++)
            {
                var handle = new Rectangle
                {
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.White,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 1,
                    Cursor = cursors[i],
                    Tag = i // Store index
                };

                handle.MouseLeftButtonDown += Handle_MouseLeftButtonDown;
                handle.MouseLeftButtonUp += Handle_MouseLeftButtonUp;
                handle.MouseMove += Handle_MouseMove;

                _resizeHandles[i] = handle;
                _handleContainer.Children.Add(handle);
            }

            PositionResizeHandles();
        }

        private bool _isResizing;
        private int _resizeHandleIndex;
        private Point _resizeStartPoint;
        private double _resizeStartWidth;
        private double _resizeStartHeight;
        private double _resizeStartX;
        private double _resizeStartY;
        private double _resizeStartCenterX;
        private double _resizeStartCenterY;

        private void Handle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle handle)
            {
                _isResizing = true;
                _resizeHandleIndex = (int)handle.Tag;

                // Get mouse position - EditorCanvas IS the artboard now, so coordinates are direct
                _resizeStartPoint = e.GetPosition(EditorCanvas ?? Parent as UIElement);

                _resizeStartWidth = ViewModel.Width;
                _resizeStartHeight = ViewModel.Height;
                _resizeStartX = ViewModel.X;
                _resizeStartY = ViewModel.Y;
                _resizeStartCenterX = ViewModel.X + ViewModel.Width / 2;
                _resizeStartCenterY = ViewModel.Y + ViewModel.Height / 2;
                handle.CaptureMouse();
                e.Handled = true;

                // Notify resize started for spacing guides
                DragStateChanged?.Invoke(this, new DragStateEventArgs(ViewModel, false, true));
            }
        }

        private void Handle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizing && sender is Rectangle handle)
            {
                _isResizing = false;
                handle.ReleaseMouseCapture();
                ElementChanged?.Invoke(this, new ElementChangedEventArgs(
                    ViewModel, true,
                    _resizeStartX, _resizeStartY,
                    _resizeStartWidth, _resizeStartHeight));

                // Notify resize ended for spacing guides
                DragStateChanged?.Invoke(this, new DragStateEventArgs(ViewModel, false, false));
            }
        }

        private void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizing && e.LeftButton == MouseButtonState.Pressed)
            {
                // EditorCanvas IS the artboard - coordinates are direct
                Point currentPoint = e.GetPosition(EditorCanvas ?? Parent as UIElement);
                double deltaX = currentPoint.X - _resizeStartPoint.X;
                double deltaY = currentPoint.Y - _resizeStartPoint.Y;

                const double minSize = 10;

                // Check if Shift is held for aspect ratio lock
                bool shiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
                // Check if Alt is held for scale from center (like Photoshop)
                bool altPressed = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
                double aspectRatio = _resizeStartWidth / _resizeStartHeight;

                switch (_resizeHandleIndex)
                {
                    case 0: // Top-left
                        if (shiftPressed)
                        {
                            // Use the larger delta to determine size, maintain aspect ratio
                            double newWidth = Math.Max(minSize, _resizeStartWidth - deltaX);
                            double newHeight = newWidth / aspectRatio;
                            if (newHeight < minSize) { newHeight = minSize; newWidth = newHeight * aspectRatio; }
                            ViewModel.Width = newWidth;
                            ViewModel.Height = newHeight;
                        }
                        else
                        {
                            ViewModel.Width = Math.Max(minSize, _resizeStartWidth - deltaX);
                            ViewModel.Height = Math.Max(minSize, _resizeStartHeight - deltaY);
                        }
                        ViewModel.X = _resizeStartX + (_resizeStartWidth - ViewModel.Width);
                        ViewModel.Y = _resizeStartY + (_resizeStartHeight - ViewModel.Height);
                        break;

                    case 1: // Top-right
                        if (shiftPressed)
                        {
                            double newWidth = Math.Max(minSize, _resizeStartWidth + deltaX);
                            double newHeight = newWidth / aspectRatio;
                            if (newHeight < minSize) { newHeight = minSize; newWidth = newHeight * aspectRatio; }
                            ViewModel.Width = newWidth;
                            ViewModel.Height = newHeight;
                        }
                        else
                        {
                            ViewModel.Width = Math.Max(minSize, _resizeStartWidth + deltaX);
                            ViewModel.Height = Math.Max(minSize, _resizeStartHeight - deltaY);
                        }
                        ViewModel.Y = _resizeStartY + (_resizeStartHeight - ViewModel.Height);
                        break;

                    case 2: // Bottom-left
                        if (shiftPressed)
                        {
                            double newWidth = Math.Max(minSize, _resizeStartWidth - deltaX);
                            double newHeight = newWidth / aspectRatio;
                            if (newHeight < minSize) { newHeight = minSize; newWidth = newHeight * aspectRatio; }
                            ViewModel.Width = newWidth;
                            ViewModel.Height = newHeight;
                        }
                        else
                        {
                            ViewModel.Width = Math.Max(minSize, _resizeStartWidth - deltaX);
                            ViewModel.Height = Math.Max(minSize, _resizeStartHeight + deltaY);
                        }
                        ViewModel.X = _resizeStartX + (_resizeStartWidth - ViewModel.Width);
                        break;

                    case 3: // Bottom-right
                        if (shiftPressed)
                        {
                            double newWidth = Math.Max(minSize, _resizeStartWidth + deltaX);
                            double newHeight = newWidth / aspectRatio;
                            if (newHeight < minSize) { newHeight = minSize; newWidth = newHeight * aspectRatio; }
                            ViewModel.Width = newWidth;
                            ViewModel.Height = newHeight;
                        }
                        else
                        {
                            ViewModel.Width = Math.Max(minSize, _resizeStartWidth + deltaX);
                            ViewModel.Height = Math.Max(minSize, _resizeStartHeight + deltaY);
                        }
                        break;

                    case 4: // Top (edge - only height changes, no aspect ratio)
                        ViewModel.Height = Math.Max(minSize, _resizeStartHeight - deltaY);
                        ViewModel.Y = _resizeStartY + (_resizeStartHeight - ViewModel.Height);
                        break;

                    case 5: // Bottom (edge - only height changes, no aspect ratio)
                        ViewModel.Height = Math.Max(minSize, _resizeStartHeight + deltaY);
                        break;

                    case 6: // Left (edge - only width changes, no aspect ratio)
                        ViewModel.Width = Math.Max(minSize, _resizeStartWidth - deltaX);
                        ViewModel.X = _resizeStartX + (_resizeStartWidth - ViewModel.Width);
                        break;

                    case 7: // Right (edge - only width changes, no aspect ratio)
                        ViewModel.Width = Math.Max(minSize, _resizeStartWidth + deltaX);
                        break;
                }

                // Alt+resize: scale from center (Photoshop-style)
                // Apply symmetric resize by doubling the delta and centering
                if (altPressed)
                {
                    // Calculate how much size changed from original
                    double widthDelta = ViewModel.Width - _resizeStartWidth;
                    double heightDelta = ViewModel.Height - _resizeStartHeight;

                    // Double the change (resize affects both sides equally)
                    ViewModel.Width = Math.Max(minSize, _resizeStartWidth + widthDelta * 2);
                    ViewModel.Height = Math.Max(minSize, _resizeStartHeight + heightDelta * 2);

                    // Reposition to keep center fixed
                    ViewModel.X = _resizeStartCenterX - ViewModel.Width / 2;
                    ViewModel.Y = _resizeStartCenterY - ViewModel.Height / 2;
                }

                // Clamp to artboard bounds after resizing (unless overflow is allowed)
                if (GetArtboardBounds != null && !ViewModel.AllowCanvasOverflow)
                {
                    var bounds = GetArtboardBounds();

                    // Clamp position to stay within bounds
                    ViewModel.X = Math.Max(0, Math.Min(ViewModel.X, bounds.Width - ViewModel.Width));
                    ViewModel.Y = Math.Max(0, Math.Min(ViewModel.Y, bounds.Height - ViewModel.Height));

                    // Clamp size to not exceed bounds from current position
                    ViewModel.Width = Math.Min(ViewModel.Width, bounds.Width - ViewModel.X);
                    ViewModel.Height = Math.Min(ViewModel.Height, bounds.Height - ViewModel.Y);
                }

                // Update spacing guides during resize
                DragStateChanged?.Invoke(this, new DragStateEventArgs(ViewModel, false, true));
            }
        }

        private void PositionResizeHandles()
        {
            if (_resizeHandles == null) return;

            double w = ViewModel.Width;
            double h = ViewModel.Height;

            // Corners
            SetLeft(_resizeHandles[0], -4); SetTop(_resizeHandles[0], -4); // Top-left
            SetLeft(_resizeHandles[1], w - 4); SetTop(_resizeHandles[1], -4); // Top-right
            SetLeft(_resizeHandles[2], -4); SetTop(_resizeHandles[2], h - 4); // Bottom-left
            SetLeft(_resizeHandles[3], w - 4); SetTop(_resizeHandles[3], h - 4); // Bottom-right

            // Edges
            SetLeft(_resizeHandles[4], w / 2 - 4); SetTop(_resizeHandles[4], -4); // Top
            SetLeft(_resizeHandles[5], w / 2 - 4); SetTop(_resizeHandles[5], h - 4); // Bottom
            SetLeft(_resizeHandles[6], -4); SetTop(_resizeHandles[6], h / 2 - 4); // Left
            SetLeft(_resizeHandles[7], w - 4); SetTop(_resizeHandles[7], h / 2 - 4); // Right
        }

        private Brush GetEditorHintBrush()
        {
            // Editor hint colors - delegated to handler
            return _handler.GetHintBrush();
        }

        public void UpdatePosition()
        {
            // SIMPLIFIED: Artboard IS the coordinate system now
            // - ViewModel stores LOCAL coordinates (relative to parent)
            // - For root elements: X,Y is position in artboard
            // - For nested elements: X,Y is position relative to parent's ChildContainer
            // - WPF layout handles the rest automatically

            SetLeft(this, ViewModel.X);
            SetTop(this, ViewModel.Y);
        }

        private void UpdateSize()
        {
            Width = ViewModel.Width;
            Height = ViewModel.Height;
            _mainRect.Width = ViewModel.Width;
            _mainRect.Height = ViewModel.Height;

            // Delegate element-specific size updates to handler
            _handler.UpdateSize(this, ViewModel);

            PositionResizeHandles();
        }

        /// <summary>
        /// Recalculates and applies layout positions for child elements based on container type.
        /// Delegates to handler for element-specific layout logic.
        /// </summary>
        public void LayoutChildren()
        {
            _handler.LayoutChildren(this, ViewModel);
        }

        /// <summary>
        /// Updates element content (images, text, etc.). Called after callbacks are wired up.
        /// </summary>
        public void UpdateContent()
        {
            _handler.UpdateContent(this, ViewModel);
        }

        /// <summary>
        /// Updates image content. Delegates to handler's UpdateContent.
        /// </summary>
        public void UpdateImageContent()
        {
            _handler.UpdateContent(this, ViewModel);
        }

        /// <summary>
        /// Updates text content. Delegates to handler's UpdateContent.
        /// </summary>
        public void UpdateTextContent()
        {
            _handler.UpdateContent(this, ViewModel);
        }

        /// <summary>
        /// Request the parent visual to re-layout its children
        /// </summary>
        private void RequestParentLayout()
        {
            if (Parent is UIElementVisual parentVisual)
            {
                parentVisual.LayoutChildren();
            }
        }

        private void UpdateLockedVisual()
        {
            if (ViewModel.IsLocked)
            {
                // Make entire visual pass-through for mouse events
                // This allows clicking on elements underneath the locked overlay
                this.IsHitTestVisible = false;

                // Create lock icon if it doesn't exist
                if (_lockIcon == null)
                {
                    // Lock icon path: a padlock shape
                    _lockIcon = new System.Windows.Shapes.Path
                    {
                        // Simple lock icon: body + shackle
                        Data = Geometry.Parse("M 4,8 L 4,5 C 4,2 6,0 8,0 C 10,0 12,2 12,5 L 12,8 M 2,8 L 14,8 L 14,16 L 2,16 Z"),
                        Fill = new SolidColorBrush(Color.FromArgb(200, 255, 165, 0)), // Orange
                        Stroke = new SolidColorBrush(Color.FromRgb(180, 120, 0)),
                        StrokeThickness = 0.5,
                        IsHitTestVisible = false,
                        RenderTransform = new ScaleTransform(0.8, 0.8)
                    };
                    Children.Add(_lockIcon);
                }

                // Position in top-right corner
                Canvas.SetRight(_lockIcon, 4);
                Canvas.SetTop(_lockIcon, 4);
                Canvas.SetLeft(_lockIcon, ViewModel.Width - 18);
                Panel.SetZIndex(_lockIcon, 1000); // Above everything
                _lockIcon.Visibility = Visibility.Visible;
            }
            else
            {
                // Re-enable hit testing when unlocked
                this.IsHitTestVisible = true;

                // Hide lock icon
                if (_lockIcon != null)
                {
                    _lockIcon.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void UpdateVisibility()
        {
            // Map Stride visibility values to WPF Visibility
            this.Visibility = ViewModel.Visibility switch
            {
                "Hidden" => Visibility.Hidden,
                "Collapsed" => Visibility.Collapsed,
                _ => Visibility.Visible
            };
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            // Skip hover highlighting for locked elements (e.g., overlay visors)
            if (ViewModel.IsLocked)
                return;

            if (!ViewModel.IsSelected)
            {
                _mainRect.Stroke = Brushes.Gray;
                _mainRect.StrokeThickness = 1;

                // Show editor hint overlay if element has no background color
                // Check alpha only - Colors.Transparent is #00FFFFFF but file might have #00000000
                if (ViewModel.BackgroundColor.A == 0)
                {
                    _mainRect.Fill = GetEditorHintBrush();
                }
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (!ViewModel.IsSelected)
            {
                _mainRect.Stroke = Brushes.Transparent;
                _mainRect.StrokeThickness = 1;

                // Remove editor hint overlay, restore actual background
                _mainRect.Fill = _handler.GetFillBrush(ViewModel);
            }
        }

        private void UpdateSelectionVisual()
        {
            if (ViewModel.IsSelected)
            {
                // When force-hidden (multi-select group overlay is showing), hide all selection visuals
                if (_forceHideSelectionVisuals)
                {
                    _border.BorderBrush = Brushes.Transparent;
                    _mainRect.StrokeThickness = 1;
                    _mainRect.Stroke = Brushes.Transparent;
                    _mainRect.Fill = _handler.GetFillBrush(ViewModel);
                    _handleContainer.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _border.BorderBrush = Brushes.Blue;
                    _mainRect.StrokeThickness = 2;
                    _mainRect.Stroke = Brushes.Blue;

                    // When selected, show real background OR editor hint if no background
                    // Check alpha only - Colors.Transparent is #00FFFFFF but file might have #00000000
                    if (ViewModel.BackgroundColor.A == 0)
                    {
                        _mainRect.Fill = GetEditorHintBrush(); // Show editor hint for empty elements
                    }
                    else
                    {
                        _mainRect.Fill = _handler.GetFillBrush(ViewModel); // Show actual background color
                    }

                    _handleContainer.Visibility = Visibility.Visible;
                }
            }
            else
            {
                _border.BorderBrush = Brushes.Transparent;
                _mainRect.StrokeThickness = 1;
                _mainRect.Stroke = Brushes.Transparent;

                // When not selected, only show real background (not editor hints)
                _mainRect.Fill = _handler.GetFillBrush(ViewModel);

                _handleContainer.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Sets whether the selection visuals (border + handles) should be visible.
        /// When multiple elements are selected, individual selection visuals are hidden
        /// and the group selection overlay shows the unified selection instead.
        /// </summary>
        public void SetHandlesVisible(bool visible)
        {
            _forceHideSelectionVisuals = !visible;
            UpdateSelectionVisual();
        }

        private bool _wasAlreadySelected; // Track if element was selected before mouse down

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // If element is locked, ignore canvas clicks (selection still works via hierarchy tree)
            if (ViewModel.IsLocked)
            {
                e.Handled = false; // Let click pass through to elements behind
                return;
            }

            // Store start position for undo
            _elementStartPos = new Point(ViewModel.X, ViewModel.Y);
            _elementStartWidth = ViewModel.Width;
            _elementStartHeight = ViewModel.Height;

            // Remember if this element was already selected (for Figma-style selection logic)
            _wasAlreadySelected = ViewModel.IsSelected;

            // SIMPLIFIED: EditorCanvas IS the artboard now
            // Get mouse position in artboard coordinates (direct, no offset needed)
            Point mousePos = e.GetPosition(EditorCanvas ?? Parent as UIElement);

            // Store drag start point for threshold check
            _dragStartPoint = mousePos;

            // Calculate element's world position by adding parent positions
            double elementWorldX = ViewModel.X;
            double elementWorldY = ViewModel.Y;
            var currentParent = ViewModel.Parent;
            while (currentParent != null && !currentParent.IsSystemElement)
            {
                elementWorldX += currentParent.X;
                elementWorldY += currentParent.Y;
                currentParent = currentParent.Parent;
            }

            // Offset from mouse to element top-left (to maintain grab point during drag)
            _dragOffset = new Point(mousePos.X - elementWorldX, mousePos.Y - elementWorldY);

            // Mouse is down, but drag hasn't started yet (need to exceed threshold)
            _isDragging = true;
            _isDragStarted = false;
            _altDragDuplicateTriggered = false;
            CaptureMouse();
            e.Handled = true;

            // Figma-style selection:
            // - If element is NOT selected, select it immediately (so drag works)
            // - If element IS already selected, don't change selection (allow drag without reselect)
            // - Ctrl+click always toggles selection immediately (multi-select)
            bool ctrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            bool shiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;

            if (ctrlPressed || !_wasAlreadySelected)
            {
                ElementSelected?.Invoke(this, new ElementSelectedEventArgs(ViewModel, ctrlPressed, shiftPressed));
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ReleaseMouseCapture();

                // Only fire change event if drag actually started (element was moved)
                if (_isDragStarted)
                {
                    ElementChanged?.Invoke(this, new ElementChangedEventArgs(
                        ViewModel, false,
                        _elementStartPos.X, _elementStartPos.Y,
                        _elementStartWidth, _elementStartHeight));

                    // Notify drag ended for spacing guides
                    DragStateChanged?.Invoke(this, new DragStateEventArgs(ViewModel, false, false));
                }

                _isDragStarted = false;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                // SIMPLIFIED: EditorCanvas IS the artboard - coordinates are direct
                if (EditorCanvas != null)
                {
                    Point mousePos = e.GetPosition(EditorCanvas);

                    // Check drag threshold before actually moving the element
                    // This prevents accidental moves when just clicking to select
                    if (!_isDragStarted)
                    {
                        double thresholdDeltaX = Math.Abs(mousePos.X - _dragStartPoint.X);
                        double thresholdDeltaY = Math.Abs(mousePos.Y - _dragStartPoint.Y);

                        // Use system drag threshold (typically 4 pixels)
                        if (thresholdDeltaX < SystemParameters.MinimumHorizontalDragDistance &&
                            thresholdDeltaY < SystemParameters.MinimumVerticalDragDistance)
                        {
                            return; // Not dragging yet, just a click
                        }

                        // Threshold exceeded - start the actual drag
                        // But NOT for button content - Stride doesn't allow free positioning of button content
                        // Content position is controlled only by alignment (Left/Center/Right, Top/Center/Bottom)
                        if (ViewModel.IsButtonContent)
                        {
                            return; // Button content cannot be dragged
                        }

                        _isDragStarted = true;

                        // Alt+drag to duplicate (Photoshop style) - trigger once when drag starts
                        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0 && !_altDragDuplicateTriggered)
                        {
                            _altDragDuplicateTriggered = true;
                            AltDragDuplicate?.Invoke(this, ViewModel);
                        }

                        // Notify drag started for spacing guides
                        DragStateChanged?.Invoke(this, new DragStateEventArgs(ViewModel, true, false));
                    }

                    // Calculate new world position (mouse - offset to maintain grab point)
                    double newWorldX = mousePos.X - _dragOffset.X;
                    double newWorldY = mousePos.Y - _dragOffset.Y;

                    // Only apply snapping if Ctrl is NOT pressed (smooth drag with Ctrl)
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                    {
                        // Apply grid snap first
                        if (SnapValue != null)
                        {
                            newWorldX = SnapValue(newWorldX);
                            newWorldY = SnapValue(newWorldY);
                        }

                        // Apply center snap (magnet to artboard center)
                        if (CenterSnap != null)
                        {
                            var elementRect = new Rect(newWorldX, newWorldY, ViewModel.Width, ViewModel.Height);
                            var (snappedX, snappedY, snapH, snapV) = CenterSnap(elementRect);

                            // Only apply center snap if element is close enough (the callback handles threshold)
                            if (snapH) newWorldX = snappedX;
                            if (snapV) newWorldY = snappedY;
                        }
                    }

                    // Convert world position to local coordinates for the ViewModel
                    double localX = newWorldX;
                    double localY = newWorldY;

                    // Subtract parent positions to get local coordinates
                    var currentParent = ViewModel.Parent;
                    while (currentParent != null && !currentParent.IsSystemElement)
                    {
                        localX -= currentParent.X;
                        localY -= currentParent.Y;
                        currentParent = currentParent.Parent;
                    }

                    // Calculate delta from original position for multi-select
                    double deltaX = localX - ViewModel.X;
                    double deltaY = localY - ViewModel.Y;

                    // Let MainWindow handle multi-select drag - it returns true if we should NOT move ourselves
                    // (because a selected parent will move us)
                    bool parentWillMoveUs = HandleMultiSelectDrag?.Invoke(ViewModel.Id, deltaX, deltaY) ?? false;

                    if (!parentWillMoveUs)
                    {
                        // Only clamp to artboard bounds for root elements (no parent)
                        // Child elements can be positioned outside their parent bounds intentionally
                        // Also skip clamping if element has AllowCanvasOverflow enabled
                        bool isRootElement = ViewModel.Parent == null || ViewModel.Parent.IsSystemElement;

                        if (isRootElement && GetArtboardBounds != null && !ViewModel.AllowCanvasOverflow)
                        {
                            var bounds = GetArtboardBounds();
                            localX = Math.Max(0, Math.Min(localX, bounds.Width - ViewModel.Width));
                            localY = Math.Max(0, Math.Min(localY, bounds.Height - ViewModel.Height));
                        }

                        ViewModel.X = localX;
                        ViewModel.Y = localY;
                    }

                    // Update spacing guides during drag
                    DragStateChanged?.Invoke(this, new DragStateEventArgs(ViewModel, true, false));
                }
            }
        }
    }
}

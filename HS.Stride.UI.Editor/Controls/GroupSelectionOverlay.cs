// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HS.Stride.UI.Editor.Controls
{
    /// <summary>
    /// Event args for group resize operations
    /// </summary>
    public class GroupResizeEventArgs : EventArgs
    {
        public int HandleIndex { get; }
        public double ScaleX { get; }
        public double ScaleY { get; }
        public Point ScaleOrigin { get; }
        public Rect OriginalBounds { get; }
        public Rect NewBounds { get; }

        public GroupResizeEventArgs(int handleIndex, double scaleX, double scaleY, Point scaleOrigin, Rect originalBounds, Rect newBounds)
        {
            HandleIndex = handleIndex;
            ScaleX = scaleX;
            ScaleY = scaleY;
            ScaleOrigin = scaleOrigin;
            OriginalBounds = originalBounds;
            NewBounds = newBounds;
        }
    }

    /// <summary>
    /// Overlay control that shows a unified bounding box with resize handles for multi-selection.
    /// Similar to Photoshop's behavior when multiple layers are selected.
    /// </summary>
    public class GroupSelectionOverlay : Canvas
    {
        private Rectangle _border;
        private Rectangle[] _resizeHandles;
        private Rect _currentBounds;
        private Rect _resizeStartBounds;
        private bool _isResizing;
        private int _resizeHandleIndex;
        private Point _resizeStartPoint;

        public event EventHandler<GroupResizeEventArgs>? ResizeStarted;
        public event EventHandler<GroupResizeEventArgs>? Resizing;
        public event EventHandler<GroupResizeEventArgs>? ResizeEnded;

        /// <summary>
        /// Callback to get the current zoom level for proper mouse coordinate scaling
        /// </summary>
        public Func<double>? GetZoomLevel { get; set; }

        public GroupSelectionOverlay()
        {
            IsHitTestVisible = true;
            Visibility = Visibility.Collapsed;

            InitializeVisual();
        }

        private void InitializeVisual()
        {
            // Border rectangle (dashed blue line for group selection)
            _border = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = Brushes.Transparent,
                IsHitTestVisible = false
            };
            Children.Add(_border);

            // Create 8 resize handles (same pattern as UIElementVisual)
            CreateResizeHandles();
        }

        private void CreateResizeHandles()
        {
            _resizeHandles = new Rectangle[8];

            var cursors = new[]
            {
                Cursors.SizeNWSE, // 0: Top-left
                Cursors.SizeNESW, // 1: Top-right
                Cursors.SizeNESW, // 2: Bottom-left
                Cursors.SizeNWSE, // 3: Bottom-right
                Cursors.SizeNS,   // 4: Top
                Cursors.SizeNS,   // 5: Bottom
                Cursors.SizeWE,   // 6: Left
                Cursors.SizeWE    // 7: Right
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
                    Tag = i
                };

                handle.MouseLeftButtonDown += Handle_MouseLeftButtonDown;
                handle.MouseLeftButtonUp += Handle_MouseLeftButtonUp;
                handle.MouseMove += Handle_MouseMove;

                _resizeHandles[i] = handle;
                Children.Add(handle);
            }
        }

        /// <summary>
        /// Sets the bounds of the group selection overlay
        /// </summary>
        public void SetBounds(Rect bounds)
        {
            _currentBounds = bounds;
            Visibility = Visibility.Visible;

            // Position and size the border
            SetLeft(_border, bounds.X);
            SetTop(_border, bounds.Y);
            _border.Width = bounds.Width;
            _border.Height = bounds.Height;

            // Position resize handles
            PositionResizeHandles(bounds);
        }

        /// <summary>
        /// Hides the group selection overlay
        /// </summary>
        public void Hide()
        {
            Visibility = Visibility.Collapsed;
        }

        private void PositionResizeHandles(Rect bounds)
        {
            double x = bounds.X;
            double y = bounds.Y;
            double w = bounds.Width;
            double h = bounds.Height;

            // Corners
            SetLeft(_resizeHandles[0], x - 4); SetTop(_resizeHandles[0], y - 4);           // Top-left
            SetLeft(_resizeHandles[1], x + w - 4); SetTop(_resizeHandles[1], y - 4);       // Top-right
            SetLeft(_resizeHandles[2], x - 4); SetTop(_resizeHandles[2], y + h - 4);       // Bottom-left
            SetLeft(_resizeHandles[3], x + w - 4); SetTop(_resizeHandles[3], y + h - 4);   // Bottom-right

            // Edges
            SetLeft(_resizeHandles[4], x + w / 2 - 4); SetTop(_resizeHandles[4], y - 4);       // Top
            SetLeft(_resizeHandles[5], x + w / 2 - 4); SetTop(_resizeHandles[5], y + h - 4);   // Bottom
            SetLeft(_resizeHandles[6], x - 4); SetTop(_resizeHandles[6], y + h / 2 - 4);       // Left
            SetLeft(_resizeHandles[7], x + w - 4); SetTop(_resizeHandles[7], y + h / 2 - 4);   // Right
        }

        private void Handle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle handle)
            {
                _isResizing = true;
                _resizeHandleIndex = (int)handle.Tag;
                _resizeStartBounds = _currentBounds;
                _resizeStartPoint = e.GetPosition(this); // Use overlay as reference
                handle.CaptureMouse();
                e.Handled = true;

                // Fire resize started event
                var origin = GetScaleOrigin(_resizeHandleIndex, _resizeStartBounds);
                ResizeStarted?.Invoke(this, new GroupResizeEventArgs(
                    _resizeHandleIndex, 1.0, 1.0, origin, _resizeStartBounds, _resizeStartBounds));
            }
        }

        private void Handle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizing && sender is Rectangle handle)
            {
                _isResizing = false;
                handle.ReleaseMouseCapture();

                // Fire resize ended event
                var origin = GetScaleOrigin(_resizeHandleIndex, _resizeStartBounds);
                double scaleX = _currentBounds.Width / _resizeStartBounds.Width;
                double scaleY = _currentBounds.Height / _resizeStartBounds.Height;

                ResizeEnded?.Invoke(this, new GroupResizeEventArgs(
                    _resizeHandleIndex, scaleX, scaleY, origin, _resizeStartBounds, _currentBounds));
            }
        }

        private void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizing && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(this); // Use overlay as reference (same as start)
                double deltaX = currentPoint.X - _resizeStartPoint.X;
                double deltaY = currentPoint.Y - _resizeStartPoint.Y;

                // Check modifier keys
                bool shiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
                bool altPressed = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;

                // Calculate new bounds based on handle being dragged
                Rect newBounds = CalculateNewBounds(deltaX, deltaY, shiftPressed, altPressed);

                // Update the visual
                SetBounds(newBounds);

                // Calculate scale factors relative to the anchor point
                var origin = GetScaleOrigin(_resizeHandleIndex, _resizeStartBounds);
                double scaleX = newBounds.Width / _resizeStartBounds.Width;
                double scaleY = newBounds.Height / _resizeStartBounds.Height;

                // Adjust origin for alt (center) scaling
                if (altPressed)
                {
                    origin = new Point(
                        _resizeStartBounds.X + _resizeStartBounds.Width / 2,
                        _resizeStartBounds.Y + _resizeStartBounds.Height / 2);
                }

                // Fire resizing event
                Resizing?.Invoke(this, new GroupResizeEventArgs(
                    _resizeHandleIndex, scaleX, scaleY, origin, _resizeStartBounds, newBounds));
            }
        }

        private Rect CalculateNewBounds(double deltaX, double deltaY, bool shiftPressed, bool altPressed)
        {
            double x = _resizeStartBounds.X;
            double y = _resizeStartBounds.Y;
            double w = _resizeStartBounds.Width;
            double h = _resizeStartBounds.Height;
            double aspectRatio = w / h;
            const double minSize = 20;

            switch (_resizeHandleIndex)
            {
                case 0: // Top-left
                    if (shiftPressed)
                    {
                        double newW = Math.Max(minSize, w - deltaX);
                        double newH = newW / aspectRatio;
                        x = _resizeStartBounds.Right - newW;
                        y = _resizeStartBounds.Bottom - newH;
                        w = newW;
                        h = newH;
                    }
                    else
                    {
                        w = Math.Max(minSize, w - deltaX);
                        h = Math.Max(minSize, h - deltaY);
                        x = _resizeStartBounds.Right - w;
                        y = _resizeStartBounds.Bottom - h;
                    }
                    break;

                case 1: // Top-right
                    if (shiftPressed)
                    {
                        double newW = Math.Max(minSize, w + deltaX);
                        double newH = newW / aspectRatio;
                        y = _resizeStartBounds.Bottom - newH;
                        w = newW;
                        h = newH;
                    }
                    else
                    {
                        w = Math.Max(minSize, w + deltaX);
                        h = Math.Max(minSize, h - deltaY);
                        y = _resizeStartBounds.Bottom - h;
                    }
                    break;

                case 2: // Bottom-left
                    if (shiftPressed)
                    {
                        double newW = Math.Max(minSize, w - deltaX);
                        double newH = newW / aspectRatio;
                        x = _resizeStartBounds.Right - newW;
                        w = newW;
                        h = newH;
                    }
                    else
                    {
                        w = Math.Max(minSize, w - deltaX);
                        h = Math.Max(minSize, h + deltaY);
                        x = _resizeStartBounds.Right - w;
                    }
                    break;

                case 3: // Bottom-right
                    if (shiftPressed)
                    {
                        double newW = Math.Max(minSize, w + deltaX);
                        double newH = newW / aspectRatio;
                        w = newW;
                        h = newH;
                    }
                    else
                    {
                        w = Math.Max(minSize, w + deltaX);
                        h = Math.Max(minSize, h + deltaY);
                    }
                    break;

                case 4: // Top (edge)
                    h = Math.Max(minSize, h - deltaY);
                    y = _resizeStartBounds.Bottom - h;
                    break;

                case 5: // Bottom (edge)
                    h = Math.Max(minSize, h + deltaY);
                    break;

                case 6: // Left (edge)
                    w = Math.Max(minSize, w - deltaX);
                    x = _resizeStartBounds.Right - w;
                    break;

                case 7: // Right (edge)
                    w = Math.Max(minSize, w + deltaX);
                    break;
            }

            // Alt: scale from center
            if (altPressed)
            {
                double centerX = _resizeStartBounds.X + _resizeStartBounds.Width / 2;
                double centerY = _resizeStartBounds.Y + _resizeStartBounds.Height / 2;

                // Calculate how much size changed
                double widthDelta = w - _resizeStartBounds.Width;
                double heightDelta = h - _resizeStartBounds.Height;

                // Double the change (resize affects both sides)
                w = Math.Max(minSize, _resizeStartBounds.Width + widthDelta * 2);
                h = Math.Max(minSize, _resizeStartBounds.Height + heightDelta * 2);

                // Recenter
                x = centerX - w / 2;
                y = centerY - h / 2;
            }

            return new Rect(x, y, w, h);
        }

        /// <summary>
        /// Gets the scale origin (anchor point) for a given handle index.
        /// The origin is the opposite corner/edge that stays fixed during resize.
        /// </summary>
        private Point GetScaleOrigin(int handleIndex, Rect bounds)
        {
            return handleIndex switch
            {
                0 => new Point(bounds.Right, bounds.Bottom),   // Top-left: anchor at bottom-right
                1 => new Point(bounds.Left, bounds.Bottom),    // Top-right: anchor at bottom-left
                2 => new Point(bounds.Right, bounds.Top),      // Bottom-left: anchor at top-right
                3 => new Point(bounds.Left, bounds.Top),       // Bottom-right: anchor at top-left
                4 => new Point(bounds.X + bounds.Width / 2, bounds.Bottom), // Top: anchor at bottom center
                5 => new Point(bounds.X + bounds.Width / 2, bounds.Top),    // Bottom: anchor at top center
                6 => new Point(bounds.Right, bounds.Y + bounds.Height / 2), // Left: anchor at right center
                7 => new Point(bounds.Left, bounds.Y + bounds.Height / 2),  // Right: anchor at left center
                _ => new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2)
            };
        }
    }
}

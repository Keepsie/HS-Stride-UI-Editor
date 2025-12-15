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
    /// A draggable guide line (horizontal or vertical)
    /// </summary>
    public class Guide : Canvas
    {
        private Line _line;
        private bool _isDragging;
        private Point _dragStart;
        private bool _isAtCenter;

        // Colors for guide states
        private static readonly Brush NormalColor = new SolidColorBrush(Color.FromRgb(0, 150, 255)); // Blue
        private static readonly Brush CenterColor = new SolidColorBrush(Color.FromRgb(180, 100, 255)); // Purple

        public bool IsHorizontal { get; }
        public double Position { get; private set; }
        public double StartPosition { get; private set; }

        public event EventHandler<Guide>? GuideChanged;
        public event EventHandler<Guide>? GuideDeleted;

        public Guide(bool isHorizontal, double position)
        {
            IsHorizontal = isHorizontal;
            Position = position;

            // Transparent background is required for hit testing on the full area
            // Without this, only the 1px line itself receives mouse events
            Background = Brushes.Transparent;

            // Set cursor on the whole canvas, not just the line
            Cursor = isHorizontal ? Cursors.SizeNS : Cursors.SizeWE;

            _line = new Line
            {
                Stroke = NormalColor,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                SnapsToDevicePixels = true
            };

            Children.Add(_line);

            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseMove += OnMouseMove;
            MouseRightButtonDown += OnMouseRightButtonDown;
        }

        /// <summary>
        /// Sets whether the guide is at the center of the artboard (changes color to purple)
        /// </summary>
        public void SetCenterHighlight(bool isAtCenter)
        {
            if (_isAtCenter != isAtCenter)
            {
                _isAtCenter = isAtCenter;
                _line.Stroke = isAtCenter ? CenterColor : NormalColor;
                _line.StrokeThickness = isAtCenter ? 2 : 1;
            }
        }

        public void UpdateLayout(double canvasWidth, double canvasHeight, double zoom, double originX, double originY)
        {
            // Hit area for easier grabbing (with Background = Transparent, whole area is clickable)
            const double hitArea = 10;
            const double halfHit = hitArea / 2;

            if (IsHorizontal)
            {
                // Horizontal guide - spans full width at Y position
                var screenY = originY + (Position * zoom);

                // Line spans full width, positioned at center of hit area
                _line.X1 = 0;
                _line.Y1 = halfHit; // Center of hit area
                _line.X2 = canvasWidth;
                _line.Y2 = halfHit;

                // Position this canvas at the screen Y
                Width = canvasWidth;
                Height = hitArea;
                SetLeft(this, 0);
                SetTop(this, screenY - halfHit);
            }
            else
            {
                // Vertical guide - spans full height at X position
                var screenX = originX + (Position * zoom);

                // Line spans full height, positioned at center of hit area
                _line.X1 = halfHit; // Center of hit area
                _line.Y1 = 0;
                _line.X2 = halfHit;
                _line.Y2 = canvasHeight;

                // Position this canvas at the screen X
                Width = hitArea;
                Height = canvasHeight;
                SetLeft(this, screenX - halfHit);
                SetTop(this, 0);
            }
        }

        public void SetPosition(double position)
        {
            Position = position;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStart = e.GetPosition(Parent as UIElement);
            StartPosition = Position;
            CaptureMouse();
            e.Handled = true;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ReleaseMouseCapture();
                GuideChanged?.Invoke(this, this);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                // Calculate delta from drag start
                var currentPoint = e.GetPosition(Parent as UIElement);
                var screenDelta = IsHorizontal
                    ? currentPoint.Y - _dragStart.Y
                    : currentPoint.X - _dragStart.X;

                // Store screen delta in Tag for parent to convert to canvas coords
                Tag = screenDelta;
                GuideChanged?.Invoke(this, this);
            }
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Right-click to delete
            GuideDeleted?.Invoke(this, this);
            e.Handled = true;
        }

        public double GetDragDelta()
        {
            if (Tag is double delta)
            {
                Tag = null;
                return delta;
            }
            return 0;
        }
    }
}

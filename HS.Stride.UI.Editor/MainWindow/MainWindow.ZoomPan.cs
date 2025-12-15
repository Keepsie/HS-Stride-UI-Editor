// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HS.Stride.UI.Editor
{
    /// <summary>
    /// MainWindow partial class - Zoom and Pan functionality
    /// </summary>
    public partial class MainWindow
    {
        private void Zoom(double delta)
        {
            SetZoom(_zoomLevel + delta);
        }

        private void CanvasScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Pan with middle mouse button OR Space+left click (Photoshop style)
            if (e.MiddleButton == MouseButtonState.Pressed ||
                (_isSpaceHeld && e.LeftButton == MouseButtonState.Pressed))
            {
                _isPanning = true;
                _panStartPoint = e.GetPosition(CanvasScrollViewer);
                _panStartScrollH = CanvasScrollViewer.HorizontalOffset;
                _panStartScrollV = CanvasScrollViewer.VerticalOffset;
                CanvasScrollViewer.Cursor = Cursors.ScrollAll;
                CanvasScrollViewer.CaptureMouse();
                e.Handled = true;
            }
        }

        private void CanvasScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                var currentPoint = e.GetPosition(CanvasScrollViewer);
                var deltaX = currentPoint.X - _panStartPoint.X;
                var deltaY = currentPoint.Y - _panStartPoint.Y;

                CanvasScrollViewer.ScrollToHorizontalOffset(_panStartScrollH - deltaX);
                CanvasScrollViewer.ScrollToVerticalOffset(_panStartScrollV - deltaY);
                e.Handled = true;
            }
        }

        private void CanvasScrollViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // Stop panning on middle button release OR left button release (for Space+drag)
            if (_isPanning && (e.MiddleButton == MouseButtonState.Released || e.LeftButton == MouseButtonState.Released))
            {
                _isPanning = false;
                CanvasScrollViewer.Cursor = _isSpaceHeld ? Cursors.ScrollAll : Cursors.Arrow;
                CanvasScrollViewer.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void SetZoom(double newZoom)
        {
            _zoomLevel = Math.Clamp(newZoom, ZoomMin, ZoomMax);

            CanvasScaleTransform.ScaleX = _zoomLevel;
            CanvasScaleTransform.ScaleY = _zoomLevel;

            ZoomLevelText.Text = $"{(_zoomLevel * 100):F0}%";

            // Update rulers
            UpdateRulers();

            // Note: We don't re-center artboard here because that would move all elements
            // The artboard stays in the same canvas position, only zoom changes
        }

        private void CanvasScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateRulers();
        }

        private void UpdateRulers()
        {
            if (RootArtboard == null) return;

            // Get artboard position in canvas coordinates
            var artboardX = Canvas.GetLeft(RootArtboard);
            var artboardY = Canvas.GetTop(RootArtboard);

            // Guard against NaN
            if (double.IsNaN(artboardX)) artboardX = 0;
            if (double.IsNaN(artboardY)) artboardY = 0;

            // With RenderTransformOrigin at center (0.5, 0.5), the transform is applied around the center
            // We need to account for this when calculating screen positions
            var canvasWidth = EditorCanvas.ActualWidth;
            var canvasHeight = EditorCanvas.ActualHeight;
            var centerX = canvasWidth / 2;
            var centerY = canvasHeight / 2;

            // Screen position = (canvasPos - center) * zoom + center - scroll
            var originScreenX = (artboardX - centerX) * _zoomLevel + centerX - CanvasScrollViewer.HorizontalOffset;
            var originScreenY = (artboardY - centerY) * _zoomLevel + centerY - CanvasScrollViewer.VerticalOffset;

            if (HorizontalRuler != null)
            {
                HorizontalRuler.Zoom = _zoomLevel;
                HorizontalRuler.OriginOffset = originScreenX;
            }

            if (VerticalRuler != null)
            {
                VerticalRuler.Zoom = _zoomLevel;
                VerticalRuler.OriginOffset = originScreenY;
            }

            // Update grid to align with rulers
            UpdateGrid(originScreenX, originScreenY);

            // Update guides
            UpdateGuides();
        }

        private void UpdateGrid(double originScreenX, double originScreenY)
        {
            if (GridBrush == null) return;

            // Grid size matches snap value (default 10)
            var gridSize = GetSnapValue() * _zoomLevel;

            // Calculate offset so grid lines align with artboard origin (0,0)
            var offsetX = originScreenX % gridSize;
            var offsetY = originScreenY % gridSize;

            // Update the brush viewport
            GridBrush.Viewport = new Rect(offsetX, offsetY, gridSize, gridSize);
        }
    }
}

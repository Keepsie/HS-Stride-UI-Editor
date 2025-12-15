// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HS.Stride.UI.Editor
{
    /// <summary>
    /// MainWindow partial class - Guide creation and management
    /// </summary>
    public partial class MainWindow
    {

        private void HorizontalRuler_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Create horizontal guide - start at top of artboard (Y=0)
            // User will drag it down to desired position
            CreateGuide(true, 0);
        }

        private void VerticalRuler_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Create vertical guide - start at left of artboard (X=0)
            // User will drag it right to desired position
            CreateGuide(false, 0);
        }

        private double ScreenToCanvasX(double screenX)
        {
            var artboardX = Canvas.GetLeft(RootArtboard);
            if (double.IsNaN(artboardX)) artboardX = 0;

            var canvasWidth = EditorCanvas.ActualWidth;
            var centerX = canvasWidth / 2;
            var originScreenX = (artboardX - centerX) * _zoomLevel + centerX - CanvasScrollViewer.HorizontalOffset;

            return (screenX - originScreenX) / _zoomLevel;
        }

        private double ScreenToCanvasY(double screenY)
        {
            var artboardY = Canvas.GetTop(RootArtboard);
            if (double.IsNaN(artboardY)) artboardY = 0;

            var canvasHeight = EditorCanvas.ActualHeight;
            var centerY = canvasHeight / 2;
            var originScreenY = (artboardY - centerY) * _zoomLevel + centerY - CanvasScrollViewer.VerticalOffset;

            return (screenY - originScreenY) / _zoomLevel;
        }

        private void CreateGuide(bool isHorizontal, double position)
        {
            var guide = new Controls.Guide(isHorizontal, position);
            guide.GuideChanged += Guide_Changed;
            guide.GuideDeleted += Guide_Deleted;

            // Check if guide is at center and set highlight
            bool isAtCenter = isHorizontal
                ? Math.Abs(position - _designHeight / 2.0) < 0.1
                : Math.Abs(position - _designWidth / 2.0) < 0.1;
            guide.SetCenterHighlight(isAtCenter);

            _guides.Add(guide);
            GuidesCanvas.Children.Add(guide);

            UpdateGuides();
        }

        private void Guide_Changed(object? sender, Controls.Guide guide)
        {
            if (guide.Tag is double screenDelta)
            {
                // Convert screen delta to canvas delta and add to start position
                var canvasDelta = screenDelta / _zoomLevel;
                var newPos = guide.StartPosition + canvasDelta;

                // Snap to grid first
                newPos = SnapToGrid(newPos);

                // Check for center snap (with threshold)
                const double centerSnapThreshold = 5.0;
                bool isAtCenter = false;

                if (guide.IsHorizontal)
                {
                    // Horizontal guide - snap to vertical center (Y)
                    double centerY = _designHeight / 2.0;
                    if (Math.Abs(newPos - centerY) < centerSnapThreshold)
                    {
                        newPos = centerY;
                        isAtCenter = true;
                    }
                }
                else
                {
                    // Vertical guide - snap to horizontal center (X)
                    double centerX = _designWidth / 2.0;
                    if (Math.Abs(newPos - centerX) < centerSnapThreshold)
                    {
                        newPos = centerX;
                        isAtCenter = true;
                    }
                }

                guide.SetPosition(newPos);
                guide.SetCenterHighlight(isAtCenter);
                UpdateGuides();
            }
        }

        private void Guide_Deleted(object? sender, Controls.Guide guide)
        {
            _guides.Remove(guide);
            GuidesCanvas.Children.Remove(guide);
        }

        private void UpdateGuides()
        {
            if (GuidesCanvas == null || RootArtboard == null) return;

            var artboardX = Canvas.GetLeft(RootArtboard);
            var artboardY = Canvas.GetTop(RootArtboard);
            if (double.IsNaN(artboardX)) artboardX = 0;
            if (double.IsNaN(artboardY)) artboardY = 0;

            var canvasWidth = EditorCanvas.ActualWidth;
            var canvasHeight = EditorCanvas.ActualHeight;
            var centerX = canvasWidth / 2;
            var centerY = canvasHeight / 2;

            var originScreenX = (artboardX - centerX) * _zoomLevel + centerX - CanvasScrollViewer.HorizontalOffset;
            var originScreenY = (artboardY - centerY) * _zoomLevel + centerY - CanvasScrollViewer.VerticalOffset;

            var viewWidth = GuidesCanvas.ActualWidth;
            var viewHeight = GuidesCanvas.ActualHeight;

            foreach (var guide in _guides)
            {
                guide.UpdateLayout(viewWidth, viewHeight, _zoomLevel, originScreenX, originScreenY);
            }
        }

   
    }
}

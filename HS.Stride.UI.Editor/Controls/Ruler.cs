// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HS.Stride.UI.Editor.Controls
{
    /// <summary>
    /// A ruler control that displays measurement ticks
    /// </summary>
    public class Ruler : FrameworkElement
    {
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(Ruler),
                new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(Ruler),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ScrollOffsetProperty =
            DependencyProperty.Register(nameof(ScrollOffset), typeof(double), typeof(Ruler),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty OriginOffsetProperty =
            DependencyProperty.Register(nameof(OriginOffset), typeof(double), typeof(Ruler),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        public double ScrollOffset
        {
            get => (double)GetValue(ScrollOffsetProperty);
            set => SetValue(ScrollOffsetProperty, value);
        }

        public double OriginOffset
        {
            get => (double)GetValue(OriginOffsetProperty);
            set => SetValue(OriginOffsetProperty, value);
        }

        private readonly Pen _tickPen = new Pen(Brushes.Gray, 1);
        private readonly Typeface _typeface = new Typeface("Segoe UI");

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(30, 30, 30)), null, bounds);

            if (Orientation == Orientation.Horizontal)
                DrawHorizontalRuler(dc);
            else
                DrawVerticalRuler(dc);
        }

        private void DrawHorizontalRuler(DrawingContext dc)
        {
            double width = ActualWidth;
            double height = ActualHeight;

            // Calculate tick spacing based on zoom
            double baseSpacing = GetTickSpacing();
            double screenSpacing = baseSpacing * Zoom;

            // OriginOffset is where 0 appears in screen coordinates
            // For each screen position x, canvas coordinate = (x - OriginOffset) / Zoom

            // Find first tick position (round down to nearest tick in canvas coords)
            double startCanvasPos = Math.Floor(-OriginOffset / Zoom / baseSpacing) * baseSpacing;
            double startScreenPos = startCanvasPos * Zoom + OriginOffset;

            for (double screenX = startScreenPos; screenX < width; screenX += screenSpacing)
            {
                double canvasPos = Math.Round((screenX - OriginOffset) / Zoom);

                // Major tick every 100 units, medium every 50, minor every 10
                double tickHeight;
                if (canvasPos % 100 == 0)
                {
                    tickHeight = height * 0.7;
                    // Draw number
                    var text = new FormattedText(
                        canvasPos.ToString("0"),
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        _typeface,
                        9,
                        Brushes.Gray,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);
                    dc.DrawText(text, new Point(screenX + 2, 1));
                }
                else if (canvasPos % 50 == 0)
                {
                    tickHeight = height * 0.5;
                }
                else
                {
                    tickHeight = height * 0.3;
                }

                dc.DrawLine(_tickPen, new Point(screenX, height - tickHeight), new Point(screenX, height));
            }
        }

        private void DrawVerticalRuler(DrawingContext dc)
        {
            double width = ActualWidth;
            double height = ActualHeight;

            // Calculate tick spacing based on zoom
            double baseSpacing = GetTickSpacing();
            double screenSpacing = baseSpacing * Zoom;

            // OriginOffset is where 0 appears in screen coordinates
            // Find first tick position
            double startCanvasPos = Math.Floor(-OriginOffset / Zoom / baseSpacing) * baseSpacing;
            double startScreenPos = startCanvasPos * Zoom + OriginOffset;

            for (double screenY = startScreenPos; screenY < height; screenY += screenSpacing)
            {
                double canvasPos = Math.Round((screenY - OriginOffset) / Zoom);

                // Major tick every 100 units, medium every 50, minor every 10
                double tickWidth;
                if (canvasPos % 100 == 0)
                {
                    tickWidth = width * 0.7;
                    // Draw number horizontally (smaller font to fit)
                    var text = new FormattedText(
                        canvasPos.ToString("0"),
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        _typeface,
                        7,
                        Brushes.Gray,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    dc.DrawText(text, new Point(1, screenY + 2));
                }
                else if (canvasPos % 50 == 0)
                {
                    tickWidth = width * 0.5;
                }
                else
                {
                    tickWidth = width * 0.3;
                }

                dc.DrawLine(_tickPen, new Point(width - tickWidth, screenY), new Point(width, screenY));
            }
        }

        private double GetTickSpacing()
        {
            // Adjust spacing based on zoom to keep ticks readable
            if (Zoom < 0.25) return 100;
            if (Zoom < 0.5) return 50;
            if (Zoom < 1) return 20;
            if (Zoom < 2) return 10;
            return 5;
        }
    }
}

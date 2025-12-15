// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Controls
{
    /// <summary>
    /// Overlay that shows spacing/distance guides during drag operations.
    /// Displays lines and measurements from the dragged element to artboard edges and siblings.
    /// </summary>
    public class SpacingGuideOverlay : Canvas
    {
        private readonly List<Line> _guideLines = new();
        private readonly List<TextBlock> _guideLabels = new();
        private readonly List<Line> _siblingLines = new();
        private readonly List<TextBlock> _siblingLabels = new();

        // Guide colors
        private static readonly Brush EdgeGuideBrush = new SolidColorBrush(Color.FromRgb(255, 100, 100)); // Red for edges
        private static readonly Brush SiblingGuideBrush = new SolidColorBrush(Color.FromRgb(100, 200, 255)); // Cyan for siblings
        private static readonly Brush CenterGuideBrush = new SolidColorBrush(Color.FromRgb(255, 0, 255)); // Magenta for center guides
        private static readonly Brush LabelBackgroundBrush = new SolidColorBrush(Color.FromArgb(200, 40, 40, 40));

        // Center guide elements
        private Line? _horizontalCenterLine;
        private Line? _verticalCenterLine;

        // Snap threshold (pixels from center to trigger snap)
        private const double CenterSnapThreshold = 8.0;

        private double _artboardWidth;
        private double _artboardHeight;

        // Last snap state for detecting snap changes
        private bool _wasSnappedHorizontal = false;
        private bool _wasSnappedVertical = false;

        /// <summary>
        /// Event fired when element snaps to center (for haptic feedback)
        /// </summary>
        public event Action<bool, bool>? CenterSnapChanged; // (snappedHorizontal, snappedVertical)

        public SpacingGuideOverlay()
        {
            IsHitTestVisible = false; // Don't interfere with mouse events
            Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Set artboard dimensions for edge calculations
        /// </summary>
        public void SetArtboardSize(double width, double height)
        {
            _artboardWidth = width;
            _artboardHeight = height;
        }

        /// <summary>
        /// Show spacing guides for the element being dragged
        /// </summary>
        public void ShowGuides(UIElementViewModel draggedElement, IEnumerable<UIElementViewModel> allElements)
        {
            ClearGuides();

            if (draggedElement == null) return;

            // Get element bounds in ABSOLUTE artboard coordinates
            var (absX, absY) = draggedElement.GetAbsolutePosition();
            var elementRect = new Rect(absX, absY, draggedElement.Width, draggedElement.Height);

            // Draw edge guides (distance to artboard edges)
            DrawEdgeGuides(elementRect);

            // Draw sibling guides (distance to nearest siblings)
            DrawSiblingGuides(elementRect, draggedElement, allElements);

            // Draw center guides (when near artboard center)
            DrawCenterGuides(elementRect);

            Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Check if an element should snap to center and return the snapped position
        /// </summary>
        /// <param name="elementRect">Current element bounds</param>
        /// <param name="snappedX">Output: snapped X position (or original if no snap)</param>
        /// <param name="snappedY">Output: snapped Y position (or original if no snap)</param>
        /// <returns>Tuple of (snappedHorizontally, snappedVertically)</returns>
        public (bool snapH, bool snapV) GetCenterSnapPosition(Rect elementRect, out double snappedX, out double snappedY)
        {
            snappedX = elementRect.X;
            snappedY = elementRect.Y;

            bool snapH = false;
            bool snapV = false;

            // Artboard center
            double artboardCenterX = _artboardWidth / 2;
            double artboardCenterY = _artboardHeight / 2;

            // Element center
            double elementCenterX = elementRect.X + elementRect.Width / 2;
            double elementCenterY = elementRect.Y + elementRect.Height / 2;

            // Check horizontal center snap
            double hDist = Math.Abs(elementCenterX - artboardCenterX);
            if (hDist <= CenterSnapThreshold)
            {
                snappedX = artboardCenterX - elementRect.Width / 2;
                snapH = true;
            }

            // Check vertical center snap
            double vDist = Math.Abs(elementCenterY - artboardCenterY);
            if (vDist <= CenterSnapThreshold)
            {
                snappedY = artboardCenterY - elementRect.Height / 2;
                snapV = true;
            }

            // Fire event if snap state changed
            if (snapH != _wasSnappedHorizontal || snapV != _wasSnappedVertical)
            {
                _wasSnappedHorizontal = snapH;
                _wasSnappedVertical = snapV;
                CenterSnapChanged?.Invoke(snapH, snapV);
            }

            return (snapH, snapV);
        }

        /// <summary>
        /// Reset snap state (call when drag ends)
        /// </summary>
        public void ResetSnapState()
        {
            _wasSnappedHorizontal = false;
            _wasSnappedVertical = false;
        }

        /// <summary>
        /// Hide all spacing guides
        /// </summary>
        public void HideGuides()
        {
            Visibility = Visibility.Collapsed;
            ClearGuides();
            ResetSnapState();
        }

        private void ClearGuides()
        {
            foreach (var line in _guideLines) Children.Remove(line);
            foreach (var label in _guideLabels) Children.Remove(label);
            foreach (var line in _siblingLines) Children.Remove(line);
            foreach (var label in _siblingLabels) Children.Remove(label);

            // Clear center guides
            if (_horizontalCenterLine != null)
            {
                Children.Remove(_horizontalCenterLine);
                _horizontalCenterLine = null;
            }
            if (_verticalCenterLine != null)
            {
                Children.Remove(_verticalCenterLine);
                _verticalCenterLine = null;
            }

            _guideLines.Clear();
            _guideLabels.Clear();
            _siblingLines.Clear();
            _siblingLabels.Clear();
        }

        /// <summary>
        /// Draw center guide lines when element is near artboard center
        /// </summary>
        private void DrawCenterGuides(Rect elementRect)
        {
            // Artboard center
            double artboardCenterX = _artboardWidth / 2;
            double artboardCenterY = _artboardHeight / 2;

            // Element center
            double elementCenterX = elementRect.X + elementRect.Width / 2;
            double elementCenterY = elementRect.Y + elementRect.Height / 2;

            // Check horizontal center (vertical line)
            double hDist = Math.Abs(elementCenterX - artboardCenterX);
            if (hDist <= CenterSnapThreshold * 2) // Show guide when within 2x snap threshold
            {
                _verticalCenterLine = new Line
                {
                    X1 = artboardCenterX,
                    Y1 = 0,
                    X2 = artboardCenterX,
                    Y2 = _artboardHeight,
                    Stroke = CenterGuideBrush,
                    StrokeThickness = hDist <= CenterSnapThreshold ? 2 : 1, // Thicker when snapped
                    StrokeDashArray = hDist <= CenterSnapThreshold ? null : new DoubleCollection { 4, 4 },
                    Opacity = hDist <= CenterSnapThreshold ? 1.0 : 0.5
                };
                Children.Add(_verticalCenterLine);
            }

            // Check vertical center (horizontal line)
            double vDist = Math.Abs(elementCenterY - artboardCenterY);
            if (vDist <= CenterSnapThreshold * 2) // Show guide when within 2x snap threshold
            {
                _horizontalCenterLine = new Line
                {
                    X1 = 0,
                    Y1 = artboardCenterY,
                    X2 = _artboardWidth,
                    Y2 = artboardCenterY,
                    Stroke = CenterGuideBrush,
                    StrokeThickness = vDist <= CenterSnapThreshold ? 2 : 1, // Thicker when snapped
                    StrokeDashArray = vDist <= CenterSnapThreshold ? null : new DoubleCollection { 4, 4 },
                    Opacity = vDist <= CenterSnapThreshold ? 1.0 : 0.5
                };
                Children.Add(_horizontalCenterLine);
            }
        }

        private void DrawEdgeGuides(Rect elementRect)
        {
            // Left edge distance
            if (elementRect.Left > 1)
            {
                DrawDistanceLine(
                    0, elementRect.Top + elementRect.Height / 2,
                    elementRect.Left, elementRect.Top + elementRect.Height / 2,
                    elementRect.Left, EdgeGuideBrush, true);
            }

            // Right edge distance
            double rightDist = _artboardWidth - elementRect.Right;
            if (rightDist > 1)
            {
                DrawDistanceLine(
                    elementRect.Right, elementRect.Top + elementRect.Height / 2,
                    _artboardWidth, elementRect.Top + elementRect.Height / 2,
                    rightDist, EdgeGuideBrush, true);
            }

            // Top edge distance
            if (elementRect.Top > 1)
            {
                DrawDistanceLine(
                    elementRect.Left + elementRect.Width / 2, 0,
                    elementRect.Left + elementRect.Width / 2, elementRect.Top,
                    elementRect.Top, EdgeGuideBrush, false);
            }

            // Bottom edge distance
            double bottomDist = _artboardHeight - elementRect.Bottom;
            if (bottomDist > 1)
            {
                DrawDistanceLine(
                    elementRect.Left + elementRect.Width / 2, elementRect.Bottom,
                    elementRect.Left + elementRect.Width / 2, _artboardHeight,
                    bottomDist, EdgeGuideBrush, false);
            }
        }

        private void DrawSiblingGuides(Rect elementRect, UIElementViewModel draggedElement, IEnumerable<UIElementViewModel> allElements)
        {
            // Find siblings (elements at same level, not the dragged element itself)
            var siblings = allElements
                .Where(e => e != draggedElement &&
                           !e.IsSystemElement &&
                           e.Parent == draggedElement.Parent)
                .ToList();

            if (!siblings.Any()) return;

            // Find nearest sibling on each side
            UIElementViewModel? nearestLeft = null;
            UIElementViewModel? nearestRight = null;
            UIElementViewModel? nearestTop = null;
            UIElementViewModel? nearestBottom = null;
            Rect nearestLeftRect = Rect.Empty;
            Rect nearestRightRect = Rect.Empty;
            Rect nearestTopRect = Rect.Empty;
            Rect nearestBottomRect = Rect.Empty;

            double minLeftDist = double.MaxValue;
            double minRightDist = double.MaxValue;
            double minTopDist = double.MaxValue;
            double minBottomDist = double.MaxValue;

            foreach (var sibling in siblings)
            {
                // Use absolute position for siblings too
                var (sibAbsX, sibAbsY) = sibling.GetAbsolutePosition();
                var siblingRect = new Rect(sibAbsX, sibAbsY, sibling.Width, sibling.Height);

                // Check horizontal overlap for top/bottom guides
                bool hasHorizontalOverlap = elementRect.Left < siblingRect.Right && elementRect.Right > siblingRect.Left;

                // Check vertical overlap for left/right guides
                bool hasVerticalOverlap = elementRect.Top < siblingRect.Bottom && elementRect.Bottom > siblingRect.Top;

                // Sibling to the left
                if (hasVerticalOverlap && siblingRect.Right <= elementRect.Left)
                {
                    double dist = elementRect.Left - siblingRect.Right;
                    if (dist < minLeftDist)
                    {
                        minLeftDist = dist;
                        nearestLeft = sibling;
                        nearestLeftRect = siblingRect;
                    }
                }

                // Sibling to the right
                if (hasVerticalOverlap && siblingRect.Left >= elementRect.Right)
                {
                    double dist = siblingRect.Left - elementRect.Right;
                    if (dist < minRightDist)
                    {
                        minRightDist = dist;
                        nearestRight = sibling;
                        nearestRightRect = siblingRect;
                    }
                }

                // Sibling above
                if (hasHorizontalOverlap && siblingRect.Bottom <= elementRect.Top)
                {
                    double dist = elementRect.Top - siblingRect.Bottom;
                    if (dist < minTopDist)
                    {
                        minTopDist = dist;
                        nearestTop = sibling;
                        nearestTopRect = siblingRect;
                    }
                }

                // Sibling below
                if (hasHorizontalOverlap && siblingRect.Top >= elementRect.Bottom)
                {
                    double dist = siblingRect.Top - elementRect.Bottom;
                    if (dist < minBottomDist)
                    {
                        minBottomDist = dist;
                        nearestBottom = sibling;
                        nearestBottomRect = siblingRect;
                    }
                }
            }

            // Draw guides to nearest siblings (using absolute coordinates stored in Rect)
            if (nearestLeft != null && minLeftDist > 0 && minLeftDist < double.MaxValue)
            {
                double y = Math.Max(elementRect.Top, nearestLeftRect.Top);
                double y2 = Math.Min(elementRect.Bottom, nearestLeftRect.Bottom);
                double midY = (y + y2) / 2;

                DrawDistanceLine(
                    nearestLeftRect.Right, midY,
                    elementRect.Left, midY,
                    minLeftDist, SiblingGuideBrush, true, true);
            }

            if (nearestRight != null && minRightDist > 0 && minRightDist < double.MaxValue)
            {
                double y = Math.Max(elementRect.Top, nearestRightRect.Top);
                double y2 = Math.Min(elementRect.Bottom, nearestRightRect.Bottom);
                double midY = (y + y2) / 2;

                DrawDistanceLine(
                    elementRect.Right, midY,
                    nearestRightRect.Left, midY,
                    minRightDist, SiblingGuideBrush, true, true);
            }

            if (nearestTop != null && minTopDist > 0 && minTopDist < double.MaxValue)
            {
                double x = Math.Max(elementRect.Left, nearestTopRect.Left);
                double x2 = Math.Min(elementRect.Right, nearestTopRect.Right);
                double midX = (x + x2) / 2;

                DrawDistanceLine(
                    midX, nearestTopRect.Bottom,
                    midX, elementRect.Top,
                    minTopDist, SiblingGuideBrush, false, true);
            }

            if (nearestBottom != null && minBottomDist > 0 && minBottomDist < double.MaxValue)
            {
                double x = Math.Max(elementRect.Left, nearestBottomRect.Left);
                double x2 = Math.Min(elementRect.Right, nearestBottomRect.Right);
                double midX = (x + x2) / 2;

                DrawDistanceLine(
                    midX, elementRect.Bottom,
                    midX, nearestBottomRect.Top,
                    minBottomDist, SiblingGuideBrush, false, true);
            }
        }

        private void DrawDistanceLine(double x1, double y1, double x2, double y2, double distance, Brush brush, bool isHorizontal, bool isSibling = false)
        {
            // Main guide line
            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = brush,
                StrokeThickness = 1,
                StrokeDashArray = isSibling ? new DoubleCollection { 4, 2 } : null
            };

            Children.Add(line);
            if (isSibling)
                _siblingLines.Add(line);
            else
                _guideLines.Add(line);

            // End caps (small perpendicular lines)
            if (distance > 10)
            {
                double capSize = 6;

                if (isHorizontal)
                {
                    // Left cap
                    var leftCap = new Line
                    {
                        X1 = x1,
                        Y1 = y1 - capSize,
                        X2 = x1,
                        Y2 = y1 + capSize,
                        Stroke = brush,
                        StrokeThickness = 1
                    };
                    Children.Add(leftCap);
                    if (isSibling) _siblingLines.Add(leftCap); else _guideLines.Add(leftCap);

                    // Right cap
                    var rightCap = new Line
                    {
                        X1 = x2,
                        Y1 = y2 - capSize,
                        X2 = x2,
                        Y2 = y2 + capSize,
                        Stroke = brush,
                        StrokeThickness = 1
                    };
                    Children.Add(rightCap);
                    if (isSibling) _siblingLines.Add(rightCap); else _guideLines.Add(rightCap);
                }
                else
                {
                    // Top cap
                    var topCap = new Line
                    {
                        X1 = x1 - capSize,
                        Y1 = y1,
                        X2 = x1 + capSize,
                        Y2 = y1,
                        Stroke = brush,
                        StrokeThickness = 1
                    };
                    Children.Add(topCap);
                    if (isSibling) _siblingLines.Add(topCap); else _guideLines.Add(topCap);

                    // Bottom cap
                    var bottomCap = new Line
                    {
                        X1 = x2 - capSize,
                        Y1 = y2,
                        X2 = x2 + capSize,
                        Y2 = y2,
                        Stroke = brush,
                        StrokeThickness = 1
                    };
                    Children.Add(bottomCap);
                    if (isSibling) _siblingLines.Add(bottomCap); else _guideLines.Add(bottomCap);
                }
            }

            // Distance label (only show if distance > 5)
            if (distance >= 5)
            {
                var label = new TextBlock
                {
                    Text = Math.Round(distance).ToString(),
                    FontSize = 10,
                    Foreground = Brushes.White,
                    Background = LabelBackgroundBrush,
                    Padding = new Thickness(3, 1, 3, 1)
                };

                // Position label at center of line
                double labelX = (x1 + x2) / 2;
                double labelY = (y1 + y2) / 2;

                // Offset so label doesn't overlap the line
                if (isHorizontal)
                {
                    labelY -= 18;
                }
                else
                {
                    labelX += 4;
                }

                SetLeft(label, labelX - 10); // Approximate centering
                SetTop(label, labelY);

                Children.Add(label);
                if (isSibling)
                    _siblingLabels.Add(label);
                else
                    _guideLabels.Add(label);
            }
        }
    }
}

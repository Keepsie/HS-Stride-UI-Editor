// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HS.Stride.UI.Editor.Controls
{
    /// <summary>
    /// TextBox that supports click-and-drag to change numeric values
    /// </summary>
    public class DraggableNumericTextBox : TextBox
    {
        private bool _isDragging;
        private Point _dragStartPoint;
        private double _startValue;
        private double _dragSensitivity = 0.5; // Pixels per unit change

        public static readonly DependencyProperty StepSizeProperty =
            DependencyProperty.Register(nameof(StepSize), typeof(double), typeof(DraggableNumericTextBox),
                new PropertyMetadata(1.0));

        public double StepSize
        {
            get => (double)GetValue(StepSizeProperty);
            set => SetValue(StepSizeProperty, value);
        }

        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register(nameof(MinValue), typeof(double), typeof(DraggableNumericTextBox),
                new PropertyMetadata(double.MinValue));

        public double MinValue
        {
            get => (double)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register(nameof(MaxValue), typeof(double), typeof(DraggableNumericTextBox),
                new PropertyMetadata(double.MaxValue));

        public double MaxValue
        {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // Check if user is clicking on the text (for normal editing) or the edge area (for dragging)
            var textRect = new Rect(0, 0, ActualWidth, ActualHeight);
            var edgeMargin = 16; // Edge area for dragging

            if (e.GetPosition(this).X < textRect.Left + edgeMargin ||
                e.GetPosition(this).X > textRect.Right - edgeMargin)
            {
                // Start dragging mode
                _isDragging = true;
                _dragStartPoint = e.GetPosition(this);
                double.TryParse(Text, out _startValue);
                Cursor = Cursors.SizeWE;
                CaptureMouse();
                e.Handled = true;
            }
            else
            {
                // Normal text editing
                base.OnMouseLeftButtonDown(e);
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ReleaseMouseCapture();
                Cursor = null;
                e.Handled = true;
            }
            else
            {
                base.OnMouseLeftButtonUp(e);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(this);
                double deltaX = currentPoint.X - _dragStartPoint.X;

                // Calculate new value with step size consideration
                double newValue = _startValue + (deltaX / _dragSensitivity) * StepSize;
                newValue = Math.Max(MinValue, Math.Min(MaxValue, newValue));

                Text = newValue.ToString("F0");
                e.Handled = true;
            }
            else
            {
                // Change cursor when hovering over drag areas
                var edgeMargin = 16;
                if (e.GetPosition(this).X < edgeMargin || e.GetPosition(this).X > ActualWidth - edgeMargin)
                {
                    Cursor = Cursors.SizeWE;
                }
                else
                {
                    Cursor = null;
                }

                base.OnMouseMove(e);
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            _isDragging = false;
            Cursor = null;
            base.OnLostFocus(e);
        }
    }
}
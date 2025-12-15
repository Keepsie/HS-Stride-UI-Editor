// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Controls.Elements
{
    /// <summary>
    /// Base class for element handlers with shared functionality
    /// </summary>
    public abstract class ElementHandlerBase : IElementHandler
    {
        public abstract string ElementType { get; }

        public virtual double DefaultWidth => 100;
        public virtual double DefaultHeight => 100;
        public virtual Color DefaultBackgroundColor => Colors.Transparent;

        public virtual void InitializeVisual(UIElementVisual visual, UIElementViewModel viewModel)
        {
            // Base implementation - subclasses add element-specific visuals
        }

        public virtual void UpdateContent(UIElementVisual visual, UIElementViewModel viewModel)
        {
            // Base implementation - subclasses update element-specific content
        }

        public virtual void UpdateSize(UIElementVisual visual, UIElementViewModel viewModel)
        {
            // Base implementation - subclasses handle size-dependent updates
        }

        public virtual Brush GetFillBrush(UIElementViewModel viewModel)
        {
            // Default: transparent background
            if (viewModel.BackgroundColor != Colors.Transparent)
            {
                return new SolidColorBrush(viewModel.BackgroundColor);
            }
            return Brushes.Transparent;
        }

        public virtual Brush GetHintBrush()
        {
            // Default editor hint color - semi-transparent blue
            return new SolidColorBrush(Color.FromArgb(30, 100, 150, 255));
        }

        public virtual IEnumerable<string> GetPropertyCategories()
        {
            yield return "Layout";
            yield return "Appearance";
        }

        public virtual void LayoutChildren(UIElementVisual visual, UIElementViewModel viewModel)
        {
            // Default: no special layout (Canvas behavior)
        }

        public virtual bool HandlePropertyChange(UIElementVisual visual, UIElementViewModel viewModel, string propertyName)
        {
            // Base implementation doesn't handle any specific properties
            return false;
        }

        #region Helper Methods

        protected static TextBlock CreateTextBlock(string text, double fontSize = 14, FontWeight? fontWeight = null)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = fontSize,
                FontWeight = fontWeight ?? FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        protected static Border CreateBorder(Color borderColor, double thickness = 1, double cornerRadius = 0)
        {
            return new Border
            {
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(thickness),
                CornerRadius = new CornerRadius(cornerRadius),
                Background = Brushes.Transparent
            };
        }

        protected static Image CreateImage(Stretch stretch = Stretch.Fill)
        {
            return new Image { Stretch = stretch };
        }

        protected static Rectangle CreateRectangle(Brush fill, double radiusX = 0, double radiusY = 0)
        {
            return new Rectangle
            {
                Fill = fill,
                RadiusX = radiusX,
                RadiusY = radiusY
            };
        }

        protected static void PositionElement(UIElement element, double left, double top)
        {
            Canvas.SetLeft(element, left);
            Canvas.SetTop(element, top);
        }

        protected static void SizeElement(FrameworkElement element, double width, double height)
        {
            // Clamp to minimum of 1 to prevent WPF InvalidPropertyValue errors
            element.Width = Math.Max(1, width);
            element.Height = Math.Max(1, height);
        }

        protected static VerticalAlignment ConvertVerticalAlignment(string alignment)
        {
            return alignment switch
            {
                "Top" => VerticalAlignment.Top,
                "Center" => VerticalAlignment.Center,
                "Bottom" => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Stretch
            };
        }

        protected static HorizontalAlignment ConvertHorizontalAlignment(string alignment)
        {
            return alignment switch
            {
                "Left" => HorizontalAlignment.Left,
                "Center" => HorizontalAlignment.Center,
                "Right" => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Stretch
            };
        }

        protected static TextAlignment ConvertTextAlignment(string alignment)
        {
            return alignment switch
            {
                "Left" => TextAlignment.Left,
                "Center" => TextAlignment.Center,
                "Right" => TextAlignment.Right,
                "Justify" => TextAlignment.Justify,
                _ => TextAlignment.Left
            };
        }

        #endregion
    }
}

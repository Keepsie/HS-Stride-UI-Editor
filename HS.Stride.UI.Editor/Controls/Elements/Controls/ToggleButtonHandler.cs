// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Controls.Elements.Controls
{
    /// <summary>
    /// Handler for ToggleButton control elements
    /// </summary>
    public class ToggleButtonHandler : ElementHandlerBase
    {
        public override string ElementType => "ToggleButton";
        public override double DefaultWidth => 120;
        public override double DefaultHeight => 30;
        public override Color DefaultBackgroundColor => Color.FromRgb(60, 60, 60);

        private const string TextKey = "ToggleText";
        private const string CheckboxKey = "ToggleCheckbox";
        private const string CheckmarkKey = "ToggleCheckmark";

        public override void InitializeVisual(UIElementVisual visual, UIElementViewModel viewModel)
        {
            base.InitializeVisual(visual, viewModel);

            // Checkbox rectangle
            var checkbox = new Rectangle
            {
                Width = 18,
                Height = 18,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                RadiusX = 3,
                RadiusY = 3,
                Tag = CheckboxKey
            };
            Canvas.SetLeft(checkbox, 4);
            Canvas.SetTop(checkbox, (viewModel.Height - 18) / 2);
            visual.Children.Add(checkbox);

            // Checkmark
            var checkmark = new Path
            {
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Visibility = viewModel.ToggleState == "Checked" ? Visibility.Visible : Visibility.Collapsed,
                Tag = CheckmarkKey
            };
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(7, 14) };
            figure.Segments.Add(new LineSegment(new Point(11, 18), true));
            figure.Segments.Add(new LineSegment(new Point(19, 8), true));
            geometry.Figures.Add(figure);
            checkmark.Data = geometry;
            Canvas.SetLeft(checkmark, 4);
            Canvas.SetTop(checkmark, (viewModel.Height - 18) / 2 - 4);
            visual.Children.Add(checkmark);

            // Text label
            var text = CreateTextBlock(viewModel.ButtonText, 14, FontWeights.Medium);
            text.Tag = TextKey;
            text.Margin = new Thickness(28, 0, 0, 0);
            text.HorizontalAlignment = HorizontalAlignment.Left;
            visual.Children.Add(text);

            UpdateContent(visual, viewModel);
        }

        public override void UpdateContent(UIElementVisual visual, UIElementViewModel viewModel)
        {
            var text = FindElement<TextBlock>(visual, TextKey);
            if (text != null)
            {
                text.Text = viewModel.ButtonText;
            }

            var checkmark = FindElement<Path>(visual, CheckmarkKey);
            if (checkmark != null)
            {
                checkmark.Visibility = viewModel.ToggleState == "Checked" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public override void UpdateSize(UIElementVisual visual, UIElementViewModel viewModel)
        {
            var checkbox = FindElement<Rectangle>(visual, CheckboxKey);
            if (checkbox != null)
            {
                Canvas.SetTop(checkbox, (viewModel.Height - 18) / 2);
            }

            var checkmark = FindElement<Path>(visual, CheckmarkKey);
            if (checkmark != null)
            {
                Canvas.SetTop(checkmark, (viewModel.Height - 18) / 2 - 4);
            }

            var text = FindElement<TextBlock>(visual, TextKey);
            if (text != null)
            {
                SizeElement(text, viewModel.Width - 28, viewModel.Height);
            }
        }

        public override bool HandlePropertyChange(UIElementVisual visual, UIElementViewModel viewModel, string propertyName)
        {
            if (propertyName == nameof(UIElementViewModel.ButtonText) ||
                propertyName == nameof(UIElementViewModel.ToggleState))
            {
                UpdateContent(visual, viewModel);
                return true;
            }
            return false;
        }

        public override Brush GetFillBrush(UIElementViewModel viewModel)
        {
            if (viewModel.BackgroundColor != Colors.Transparent)
                return new SolidColorBrush(viewModel.BackgroundColor);

            return new SolidColorBrush(DefaultBackgroundColor);
        }

        public override Brush GetHintBrush()
        {
            return new SolidColorBrush(Color.FromArgb(40, 80, 180, 80));
        }

        public override IEnumerable<string> GetPropertyCategories()
        {
            foreach (var cat in base.GetPropertyCategories())
                yield return cat;
            yield return "ToggleButton";
        }

        private static T? FindElement<T>(UIElementVisual visual, string tag) where T : FrameworkElement
        {
            return visual.Children.OfType<T>().FirstOrDefault(e => e.Tag as string == tag);
        }
    }
}

// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Controls.Elements.Controls
{
    /// <summary>
    /// Handler for EditText (text input) control elements
    /// </summary>
    public class EditTextHandler : ElementHandlerBase
    {
        public override string ElementType => "EditText";
        public override double DefaultWidth => 200;
        public override double DefaultHeight => 30;
        public override Color DefaultBackgroundColor => Color.FromRgb(35, 35, 35);

        private const string BorderKey = "EditTextBorder";
        private const string TextKey = "EditTextPlaceholder";

        public override void InitializeVisual(UIElementVisual visual, UIElementViewModel viewModel)
        {
            base.InitializeVisual(visual, viewModel);

            // Border
            var border = CreateBorder(Color.FromRgb(80, 80, 80), 1, 3);
            border.Tag = BorderKey;
            visual.Children.Add(border);

            // Placeholder/content text
            var text = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromArgb(128, 200, 200, 200)),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 4, 8, 4),
                Tag = TextKey
            };
            visual.Children.Add(text);

            UpdateContent(visual, viewModel);
        }

        public override void UpdateContent(UIElementVisual visual, UIElementViewModel viewModel)
        {
            var text = FindElement<TextBlock>(visual, TextKey);
            if (text != null)
            {
                text.Text = string.IsNullOrEmpty(viewModel.Text) ? "Enter text..." : viewModel.Text;
                text.Foreground = string.IsNullOrEmpty(viewModel.Text)
                    ? new SolidColorBrush(Color.FromArgb(128, 200, 200, 200))
                    : new SolidColorBrush(Color.FromRgb(220, 220, 220));
            }
        }

        public override void UpdateSize(UIElementVisual visual, UIElementViewModel viewModel)
        {
            var border = FindElement<Border>(visual, BorderKey);
            if (border != null)
            {
                SizeElement(border, viewModel.Width, viewModel.Height);
            }

            var text = FindElement<TextBlock>(visual, TextKey);
            if (text != null)
            {
                SizeElement(text, viewModel.Width - 16, viewModel.Height - 8);
            }
        }

        public override bool HandlePropertyChange(UIElementVisual visual, UIElementViewModel viewModel, string propertyName)
        {
            if (propertyName == nameof(UIElementViewModel.Text))
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
            yield return "EditText";
        }

        private static T? FindElement<T>(UIElementVisual visual, string tag) where T : FrameworkElement
        {
            return visual.Children.OfType<T>().FirstOrDefault(e => e.Tag as string == tag);
        }
    }
}

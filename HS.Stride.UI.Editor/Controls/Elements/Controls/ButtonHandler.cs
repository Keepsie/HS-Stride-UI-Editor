// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Controls.Elements.Controls
{
    /// <summary>
    /// Handler for Button control elements
    /// </summary>
    public class ButtonHandler : ElementHandlerBase
    {
        public override string ElementType => "Button";
        public override double DefaultWidth => 120;
        public override double DefaultHeight => 40;
        public override Color DefaultBackgroundColor => Color.FromRgb(70, 70, 70);

        private const string BorderKey = "ButtonBorder";
        private const string TextKey = "ButtonText";
        private const string ImageKey = "ButtonImage";

        public override void InitializeVisual(UIElementVisual visual, UIElementViewModel viewModel)
        {
            base.InitializeVisual(visual, viewModel);

            // Border
            var border = CreateBorder(Color.FromRgb(100, 100, 100), 1, 4);
            border.Tag = BorderKey;
            visual.Children.Add(border);

            // Image control for button images
            var image = CreateImage();
            image.Tag = ImageKey;
            image.Visibility = Visibility.Collapsed;
            visual.Children.Add(image);

            // Text label
            var text = CreateTextBlock(viewModel.ButtonText, 14, FontWeights.SemiBold);
            text.Tag = TextKey;
            text.TextAlignment = TextAlignment.Center;
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

            var image = FindElement<Image>(visual, ImageKey);
            if (image != null && visual.GetAssetImage != null)
            {
                // Try to load button image (NotPressed state for preview)
                if (!string.IsNullOrEmpty(viewModel.ButtonNotPressedImage))
                {
                    var imageSource = visual.GetAssetImage(
                        viewModel.ButtonNotPressedImage,
                        viewModel.ButtonImageMode,
                        viewModel.ButtonNotPressedFrame,
                        Colors.White);

                    if (imageSource != null)
                    {
                        image.Source = imageSource;
                        image.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        public override void UpdateSize(UIElementVisual visual, UIElementViewModel viewModel)
        {
            var border = FindElement<Border>(visual, BorderKey);
            if (border != null)
            {
                SizeElement(border, viewModel.Width, viewModel.Height);
            }

            var image = FindElement<Image>(visual, ImageKey);
            if (image != null)
            {
                SizeElement(image, viewModel.Width, viewModel.Height);
            }

            var text = FindElement<TextBlock>(visual, TextKey);
            if (text != null)
            {
                SizeElement(text, viewModel.Width, viewModel.Height);
                text.Padding = new Thickness(0, (viewModel.Height - text.FontSize - 4) / 2, 0, 0);
            }
        }

        public override bool HandlePropertyChange(UIElementVisual visual, UIElementViewModel viewModel, string propertyName)
        {
            if (propertyName == nameof(UIElementViewModel.ButtonText) ||
                propertyName == nameof(UIElementViewModel.ButtonNotPressedImage) ||
                propertyName == nameof(UIElementViewModel.ButtonNotPressedFrame) ||
                propertyName == nameof(UIElementViewModel.ButtonImageMode))
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
            return new SolidColorBrush(Color.FromArgb(40, 80, 180, 80)); // Green tint for controls
        }

        public override IEnumerable<string> GetPropertyCategories()
        {
            foreach (var cat in base.GetPropertyCategories())
                yield return cat;
            yield return "Button";
        }

        private static T? FindElement<T>(UIElementVisual visual, string tag) where T : FrameworkElement
        {
            return visual.Children.OfType<T>().FirstOrDefault(e => e.Tag as string == tag);
        }
    }
}

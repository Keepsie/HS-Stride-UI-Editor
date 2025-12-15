// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Controls.Elements.Visual
{
    /// <summary>
    /// Handler for TextBlock visual elements
    /// </summary>
    public class TextBlockHandler : ElementHandlerBase
    {
        public override string ElementType => "TextBlock";
        public override double DefaultWidth => 100;
        public override double DefaultHeight => 30;

        private const string ContainerKey = "TextContainer";
        private const string TextKey = "TextContent";

        public override void InitializeVisual(UIElementVisual visual, UIElementViewModel viewModel)
        {
            base.InitializeVisual(visual, viewModel);

            // Text content
            var text = new TextBlock
            {
                Foreground = new SolidColorBrush(viewModel.TextColor),
                FontFamily = new FontFamily(viewModel.FontFamily),
                FontSize = viewModel.FontSize,
                FontWeight = FontWeights.Bold,
                Text = viewModel.Text,
                TextWrapping = viewModel.WrapText ? TextWrapping.Wrap : TextWrapping.NoWrap,
                VerticalAlignment = ConvertVerticalAlignment(viewModel.VerticalAlignment),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(0),
                Tag = TextKey
            };

            // Container for alignment support
            var container = new Border
            {
                Width = viewModel.Width,
                Height = viewModel.Height,
                Background = Brushes.Transparent,
                Child = text,
                Tag = ContainerKey
            };

            visual.Children.Add(container);
            UpdateContent(visual, viewModel);
        }

        public override void UpdateContent(UIElementVisual visual, UIElementViewModel viewModel)
        {
            var text = FindTextBlock(visual);
            if (text == null) return;

            text.Text = viewModel.Text;
            text.Foreground = new SolidColorBrush(viewModel.TextColor);
            text.FontSize = viewModel.FontSize;
            text.TextWrapping = viewModel.WrapText ? TextWrapping.Wrap : TextWrapping.NoWrap;
            text.TextAlignment = ConvertTextAlignment(viewModel.TextAlignment);
            text.VerticalAlignment = ConvertVerticalAlignment(viewModel.VerticalAlignment);

            // Try to load custom font
            if (visual.GetFont != null && !string.IsNullOrEmpty(viewModel.FontAssetReference))
            {
                var customFont = visual.GetFont(viewModel.FontAssetReference);
                if (customFont != null)
                {
                    text.FontFamily = customFont;
                }
            }
            else
            {
                text.FontFamily = new FontFamily(viewModel.FontFamily);
            }
        }

        public override void UpdateSize(UIElementVisual visual, UIElementViewModel viewModel)
        {
            var container = FindElement<Border>(visual, ContainerKey);
            if (container != null)
            {
                SizeElement(container, viewModel.Width, viewModel.Height);
            }
        }

        public override bool HandlePropertyChange(UIElementVisual visual, UIElementViewModel viewModel, string propertyName)
        {
            if (propertyName == nameof(UIElementViewModel.Text) ||
                propertyName == nameof(UIElementViewModel.TextColor) ||
                propertyName == nameof(UIElementViewModel.FontFamily) ||
                propertyName == nameof(UIElementViewModel.FontSize) ||
                propertyName == nameof(UIElementViewModel.TextAlignment) ||
                propertyName == nameof(UIElementViewModel.WrapText) ||
                propertyName == nameof(UIElementViewModel.FontAssetReference))
            {
                UpdateContent(visual, viewModel);
                return true;
            }
            return false;
        }

        public override Brush GetHintBrush()
        {
            return new SolidColorBrush(Color.FromArgb(30, 180, 80, 180)); // Purple tint for visual elements
        }

        public override IEnumerable<string> GetPropertyCategories()
        {
            foreach (var cat in base.GetPropertyCategories())
                yield return cat;
            yield return "TextBlock";
        }

        private TextBlock? FindTextBlock(UIElementVisual visual)
        {
            var container = FindElement<Border>(visual, ContainerKey);
            return container?.Child as TextBlock;
        }

        private static T? FindElement<T>(UIElementVisual visual, string tag) where T : FrameworkElement
        {
            return visual.Children.OfType<T>().FirstOrDefault(e => e.Tag as string == tag);
        }
    }
}

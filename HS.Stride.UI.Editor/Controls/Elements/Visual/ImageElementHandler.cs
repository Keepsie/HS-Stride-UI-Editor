// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Controls.Elements.Visual
{
    /// <summary>
    /// Handler for ImageElement visual elements
    /// </summary>
    public class ImageElementHandler : ElementHandlerBase
    {
        public override string ElementType => "ImageElement";
        public override double DefaultWidth => 100;
        public override double DefaultHeight => 100;

        private const string ImageKey = "ImageContent";
        private const string TintKey = "TintOverlay";
        private const string PlaceholderKey = "ImagePlaceholder";

        public override void InitializeVisual(UIElementVisual visual, UIElementViewModel viewModel)
        {
            base.InitializeVisual(visual, viewModel);

            // Image control
            var image = new Image
            {
                Stretch = ConvertStretch(viewModel.StretchType),
                Tag = ImageKey
            };
            visual.Children.Add(image);

            // Placeholder when no image (shows element boundary)
            var placeholder = new Rectangle
            {
                Stroke = new SolidColorBrush(Color.FromArgb(60, 150, 150, 150)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = Brushes.Transparent,
                Tag = PlaceholderKey
            };
            visual.Children.Add(placeholder);

            UpdateContent(visual, viewModel);
        }

        public override void UpdateContent(UIElementVisual visual, UIElementViewModel viewModel)
        {
            var image = FindElement<Image>(visual, ImageKey);
            var placeholder = FindElement<Rectangle>(visual, PlaceholderKey);

            if (image == null) return;

            image.Stretch = ConvertStretch(viewModel.StretchType);

            if (visual.GetAssetImage != null && !string.IsNullOrEmpty(viewModel.ImageSource))
            {
                var imageSource = visual.GetAssetImage(
                    viewModel.ImageSource,
                    viewModel.ImageAssetType,
                    viewModel.SpriteFrame,
                    viewModel.ImageTintColor);

                if (imageSource != null)
                {
                    image.Source = imageSource;
                    image.Visibility = Visibility.Visible;
                    if (placeholder != null)
                        placeholder.Visibility = Visibility.Collapsed;
                    return;
                }
            }

            // No image - show placeholder
            image.Source = null;
            image.Visibility = Visibility.Collapsed;
            if (placeholder != null)
                placeholder.Visibility = Visibility.Visible;
        }

        public override void UpdateSize(UIElementVisual visual, UIElementViewModel viewModel)
        {
            var image = FindElement<Image>(visual, ImageKey);
            if (image != null)
            {
                SizeElement(image, viewModel.Width, viewModel.Height);
            }

            var placeholder = FindElement<Rectangle>(visual, PlaceholderKey);
            if (placeholder != null)
            {
                SizeElement(placeholder, viewModel.Width, viewModel.Height);
            }
        }

        public override bool HandlePropertyChange(UIElementVisual visual, UIElementViewModel viewModel, string propertyName)
        {
            if (propertyName == nameof(UIElementViewModel.ImageSource) ||
                propertyName == nameof(UIElementViewModel.ImageAssetType) ||
                propertyName == nameof(UIElementViewModel.SpriteFrame) ||
                propertyName == nameof(UIElementViewModel.ImageTintColor) ||
                propertyName == nameof(UIElementViewModel.StretchType))
            {
                UpdateContent(visual, viewModel);
                return true;
            }
            return false;
        }

        public override Brush GetHintBrush()
        {
            return new SolidColorBrush(Color.FromArgb(30, 180, 80, 180));
        }

        public override IEnumerable<string> GetPropertyCategories()
        {
            foreach (var cat in base.GetPropertyCategories())
                yield return cat;
            yield return "Image";
        }

        private static Stretch ConvertStretch(string stretchType)
        {
            return stretchType switch
            {
                "Fill" => Stretch.Fill,
                "Uniform" => Stretch.Uniform,
                "UniformToFill" => Stretch.UniformToFill,
                "None" => Stretch.None,
                _ => Stretch.Fill // FillOnStretch default
            };
        }

        private static T? FindElement<T>(UIElementVisual visual, string tag) where T : FrameworkElement
        {
            return visual.Children.OfType<T>().FirstOrDefault(e => e.Tag as string == tag);
        }
    }
}

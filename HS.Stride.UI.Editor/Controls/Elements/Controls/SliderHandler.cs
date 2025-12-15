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
    /// Handler for Slider control elements
    /// </summary>
    public class SliderHandler : ElementHandlerBase
    {
        public override string ElementType => "Slider";
        public override double DefaultWidth => 200;
        public override double DefaultHeight => 20;

        private const string TrackKey = "SliderTrack";
        private const string FillKey = "SliderFill";
        private const string ThumbKey = "SliderThumb";

        public override void InitializeVisual(UIElementVisual visual, UIElementViewModel viewModel)
        {
            base.InitializeVisual(visual, viewModel);

            // Track background
            var track = new Rectangle
            {
                Height = 4,
                Fill = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                RadiusX = 2,
                RadiusY = 2,
                Tag = TrackKey
            };
            visual.Children.Add(track);

            // Fill (value indicator)
            var fill = new Rectangle
            {
                Height = 4,
                Fill = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                RadiusX = 2,
                RadiusY = 2,
                Tag = FillKey
            };
            visual.Children.Add(fill);

            // Thumb
            var thumb = new Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                StrokeThickness = 1,
                Tag = ThumbKey
            };
            visual.Children.Add(thumb);

            UpdateContent(visual, viewModel);
        }

        public override void UpdateContent(UIElementVisual visual, UIElementViewModel viewModel)
        {
            UpdateSliderPosition(visual, viewModel);
        }

        public override void UpdateSize(UIElementVisual visual, UIElementViewModel viewModel)
        {
            UpdateSliderPosition(visual, viewModel);
        }

        public override bool HandlePropertyChange(UIElementVisual visual, UIElementViewModel viewModel, string propertyName)
        {
            if (propertyName == nameof(UIElementViewModel.SliderMinimum) ||
                propertyName == nameof(UIElementViewModel.SliderMaximum) ||
                propertyName == nameof(UIElementViewModel.SliderValue))
            {
                UpdateContent(visual, viewModel);
                return true;
            }
            return false;
        }

        public override Brush GetHintBrush()
        {
            return new SolidColorBrush(Color.FromArgb(40, 80, 180, 80));
        }

        public override IEnumerable<string> GetPropertyCategories()
        {
            foreach (var cat in base.GetPropertyCategories())
                yield return cat;
            yield return "Slider";
        }

        private void UpdateSliderPosition(UIElementVisual visual, UIElementViewModel viewModel)
        {
            var track = FindElement<Rectangle>(visual, TrackKey);
            var fill = FindElement<Rectangle>(visual, FillKey);
            var thumb = FindElement<Ellipse>(visual, ThumbKey);

            if (track == null || fill == null || thumb == null)
                return;

            double centerY = viewModel.Height / 2;
            double trackWidth = viewModel.Width - 14; // Leave space for thumb

            // Position track
            track.Width = trackWidth;
            Canvas.SetLeft(track, 7);
            Canvas.SetTop(track, centerY - 2);

            // Calculate value ratio
            double range = viewModel.SliderMaximum - viewModel.SliderMinimum;
            double ratio = range > 0 ? (viewModel.SliderValue - viewModel.SliderMinimum) / range : 0;
            ratio = Math.Clamp(ratio, 0, 1);

            // Position fill
            fill.Width = Math.Max(0, trackWidth * ratio);
            Canvas.SetLeft(fill, 7);
            Canvas.SetTop(fill, centerY - 2);

            // Position thumb
            double thumbX = 7 + (trackWidth - 14) * ratio;
            Canvas.SetLeft(thumb, thumbX);
            Canvas.SetTop(thumb, centerY - 7);
        }

        private static T? FindElement<T>(UIElementVisual visual, string tag) where T : FrameworkElement
        {
            return visual.Children.OfType<T>().FirstOrDefault(e => e.Tag as string == tag);
        }
    }
}

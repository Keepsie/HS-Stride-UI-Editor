// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Controls.Elements.Controls
{
    /// <summary>
    /// Handler for ScrollBar control elements
    /// </summary>
    public class ScrollBarHandler : ElementHandlerBase
    {
        public override string ElementType => "ScrollBar";
        public override double DefaultWidth => 20;
        public override double DefaultHeight => 100;

        private const string TrackKey = "ScrollBarTrack";
        private const string ThumbKey = "ScrollBarThumb";

        public override void InitializeVisual(UIElementVisual visual, UIElementViewModel viewModel)
        {
            base.InitializeVisual(visual, viewModel);

            // Track
            var track = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                RadiusX = 3,
                RadiusY = 3,
                Tag = TrackKey
            };
            visual.Children.Add(track);

            // Thumb
            var thumb = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                RadiusX = 3,
                RadiusY = 3,
                Tag = ThumbKey
            };
            visual.Children.Add(thumb);

            UpdateContent(visual, viewModel);
        }

        public override void UpdateContent(UIElementVisual visual, UIElementViewModel viewModel)
        {
            UpdateScrollBarPosition(visual, viewModel);
        }

        public override void UpdateSize(UIElementVisual visual, UIElementViewModel viewModel)
        {
            UpdateScrollBarPosition(visual, viewModel);
        }

        public override Brush GetHintBrush()
        {
            return new SolidColorBrush(Color.FromArgb(40, 80, 180, 80));
        }

        public override IEnumerable<string> GetPropertyCategories()
        {
            foreach (var cat in base.GetPropertyCategories())
                yield return cat;
            yield return "ScrollBar";
        }

        private void UpdateScrollBarPosition(UIElementVisual visual, UIElementViewModel viewModel)
        {
            var track = FindElement<Rectangle>(visual, TrackKey);
            var thumb = FindElement<Rectangle>(visual, ThumbKey);

            if (track == null || thumb == null)
                return;

            bool isVertical = viewModel.Height > viewModel.Width;

            if (isVertical)
            {
                track.Width = viewModel.Width - 4;
                track.Height = viewModel.Height - 4;
                Canvas.SetLeft(track, 2);
                Canvas.SetTop(track, 2);

                // Thumb (30% of track height, centered)
                thumb.Width = viewModel.Width - 8;
                thumb.Height = Math.Max(20, viewModel.Height * 0.3);
                Canvas.SetLeft(thumb, 4);
                Canvas.SetTop(thumb, (viewModel.Height - thumb.Height) / 2);
            }
            else
            {
                track.Width = viewModel.Width - 4;
                track.Height = viewModel.Height - 4;
                Canvas.SetLeft(track, 2);
                Canvas.SetTop(track, 2);

                // Thumb (30% of track width, centered)
                thumb.Width = Math.Max(20, viewModel.Width * 0.3);
                thumb.Height = viewModel.Height - 8;
                Canvas.SetLeft(thumb, (viewModel.Width - thumb.Width) / 2);
                Canvas.SetTop(thumb, 4);
            }
        }

        private static T? FindElement<T>(UIElementVisual visual, string tag) where T : System.Windows.FrameworkElement
        {
            return visual.Children.OfType<T>().FirstOrDefault(e => e.Tag as string == tag);
        }
    }
}

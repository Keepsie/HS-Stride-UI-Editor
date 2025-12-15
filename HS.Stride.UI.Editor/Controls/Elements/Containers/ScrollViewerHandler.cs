// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows.Media;
using System.Windows.Shapes;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Controls.Elements.Containers
{
    /// <summary>
    /// Handler for ScrollViewer container elements
    /// </summary>
    public class ScrollViewerHandler : ElementHandlerBase
    {
        public override string ElementType => "ScrollViewer";
        public override double DefaultWidth => 200;
        public override double DefaultHeight => 200;

        private const string ScrollBarKey = "ScrollBarHint";

        public override void InitializeVisual(UIElementVisual visual, UIElementViewModel viewModel)
        {
            base.InitializeVisual(visual, viewModel);
            UpdateScrollBarHint(visual, viewModel);
        }

        public override void UpdateContent(UIElementVisual visual, UIElementViewModel viewModel)
        {
            UpdateScrollBarHint(visual, viewModel);
        }

        public override void UpdateSize(UIElementVisual visual, UIElementViewModel viewModel)
        {
            UpdateScrollBarHint(visual, viewModel);
        }

        public override bool HandlePropertyChange(UIElementVisual visual, UIElementViewModel viewModel, string propertyName)
        {
            if (propertyName == nameof(UIElementViewModel.ScrollBarColor))
            {
                UpdateScrollBarHint(visual, viewModel);
                return true;
            }
            return false;
        }

        public override Brush GetHintBrush()
        {
            return new SolidColorBrush(Color.FromArgb(30, 100, 150, 255));
        }

        public override IEnumerable<string> GetPropertyCategories()
        {
            foreach (var cat in base.GetPropertyCategories())
                yield return cat;
            yield return "ScrollViewer";
        }

        private void UpdateScrollBarHint(UIElementVisual visual, UIElementViewModel viewModel)
        {
            // Remove existing scrollbar hints
            var existing = visual.Children.OfType<Rectangle>()
                .Where(r => r.Tag as string == ScrollBarKey)
                .ToList();
            foreach (var rect in existing)
                visual.Children.Remove(rect);

            var scrollBarColor = viewModel.ScrollBarColor;
            var brush = new SolidColorBrush(Color.FromArgb(100, scrollBarColor.R, scrollBarColor.G, scrollBarColor.B));

            // Vertical scrollbar hint
            var verticalBar = new Rectangle
            {
                Width = 8,
                Height = viewModel.Height - 12,
                Fill = brush,
                RadiusX = 4,
                RadiusY = 4,
                IsHitTestVisible = false,
                Tag = ScrollBarKey
            };
            System.Windows.Controls.Canvas.SetRight(verticalBar, 4);
            System.Windows.Controls.Canvas.SetTop(verticalBar, 4);
            System.Windows.Controls.Canvas.SetLeft(verticalBar, viewModel.Width - 12);

            // Horizontal scrollbar hint
            var horizontalBar = new Rectangle
            {
                Width = viewModel.Width - 20,
                Height = 8,
                Fill = brush,
                RadiusX = 4,
                RadiusY = 4,
                IsHitTestVisible = false,
                Tag = ScrollBarKey
            };
            System.Windows.Controls.Canvas.SetLeft(horizontalBar, 4);
            System.Windows.Controls.Canvas.SetTop(horizontalBar, viewModel.Height - 12);

            visual.Children.Add(verticalBar);
            visual.Children.Add(horizontalBar);
        }
    }
}

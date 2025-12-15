// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Controls.Elements.Containers
{
    /// <summary>
    /// Handler for StackPanel container elements
    /// </summary>
    public class StackPanelHandler : ElementHandlerBase
    {
        public override string ElementType => "StackPanel";
        public override double DefaultWidth => 200;
        public override double DefaultHeight => 200;

        private const string ArrowKey = "StackArrow";

        public override void InitializeVisual(UIElementVisual visual, UIElementViewModel viewModel)
        {
            base.InitializeVisual(visual, viewModel);
            UpdateOrientationHint(visual, viewModel);
        }

        public override void UpdateContent(UIElementVisual visual, UIElementViewModel viewModel)
        {
            UpdateOrientationHint(visual, viewModel);
        }

        public override bool HandlePropertyChange(UIElementVisual visual, UIElementViewModel viewModel, string propertyName)
        {
            if (propertyName == nameof(UIElementViewModel.StackPanelOrientation))
            {
                UpdateOrientationHint(visual, viewModel);
                LayoutChildren(visual, viewModel);
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
            yield return "StackPanel";
        }

        public override void LayoutChildren(UIElementVisual visual, UIElementViewModel viewModel)
        {
            bool isHorizontal = viewModel.StackPanelOrientation == "Horizontal";
            double offset = 0;

            foreach (var childVisual in visual.ChildContainer.Children.OfType<UIElementVisual>())
            {
                if (isHorizontal)
                {
                    Canvas.SetLeft(childVisual, offset);
                    Canvas.SetTop(childVisual, 0);
                    offset += childVisual.ViewModel.Width;
                }
                else
                {
                    Canvas.SetLeft(childVisual, 0);
                    Canvas.SetTop(childVisual, offset);
                    offset += childVisual.ViewModel.Height;
                }
            }
        }

        private void UpdateOrientationHint(UIElementVisual visual, UIElementViewModel viewModel)
        {
            // Remove existing arrow
            var existing = visual.Children.OfType<Path>()
                .FirstOrDefault(p => p.Tag as string == ArrowKey);
            if (existing != null)
                visual.Children.Remove(existing);

            bool isHorizontal = viewModel.StackPanelOrientation == "Horizontal";

            var arrow = new Path
            {
                Stroke = new SolidColorBrush(Color.FromArgb(100, 150, 200, 255)),
                StrokeThickness = 2,
                IsHitTestVisible = false,
                Tag = ArrowKey
            };

            var geometry = new PathGeometry();
            var figure = new PathFigure();

            if (isHorizontal)
            {
                // Right-pointing arrow
                double centerY = viewModel.Height / 2;
                figure.StartPoint = new Point(10, centerY);
                figure.Segments.Add(new LineSegment(new Point(viewModel.Width - 20, centerY), true));
                figure.Segments.Add(new LineSegment(new Point(viewModel.Width - 30, centerY - 8), true));
                figure.Segments.Add(new LineSegment(new Point(viewModel.Width - 20, centerY), false));
                figure.Segments.Add(new LineSegment(new Point(viewModel.Width - 30, centerY + 8), true));
            }
            else
            {
                // Down-pointing arrow
                double centerX = viewModel.Width / 2;
                figure.StartPoint = new Point(centerX, 10);
                figure.Segments.Add(new LineSegment(new Point(centerX, viewModel.Height - 20), true));
                figure.Segments.Add(new LineSegment(new Point(centerX - 8, viewModel.Height - 30), true));
                figure.Segments.Add(new LineSegment(new Point(centerX, viewModel.Height - 20), false));
                figure.Segments.Add(new LineSegment(new Point(centerX + 8, viewModel.Height - 30), true));
            }

            geometry.Figures.Add(figure);
            arrow.Data = geometry;

            visual.Children.Insert(1, arrow);
        }
    }
}

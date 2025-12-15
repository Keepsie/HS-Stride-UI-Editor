// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows.Media;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Controls.Elements.Containers
{
    /// <summary>
    /// Handler for UniformGrid container elements
    /// </summary>
    public class UniformGridHandler : ElementHandlerBase
    {
        public override string ElementType => "UniformGrid";
        public override double DefaultWidth => 200;
        public override double DefaultHeight => 200;

        public override Brush GetHintBrush()
        {
            return new SolidColorBrush(Color.FromArgb(30, 100, 150, 255));
        }

        public override IEnumerable<string> GetPropertyCategories()
        {
            foreach (var cat in base.GetPropertyCategories())
                yield return cat;
            yield return "UniformGrid";
        }

        public override void LayoutChildren(UIElementVisual visual, UIElementViewModel viewModel)
        {
            var children = visual.ChildContainer.Children.OfType<UIElementVisual>().ToList();
            if (children.Count == 0) return;

            int cols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(children.Count)));
            int rows = (int)Math.Ceiling((double)children.Count / cols);

            double cellWidth = viewModel.Width / cols;
            double cellHeight = viewModel.Height / rows;

            for (int i = 0; i < children.Count; i++)
            {
                int row = i / cols;
                int col = i % cols;

                System.Windows.Controls.Canvas.SetLeft(children[i], col * cellWidth);
                System.Windows.Controls.Canvas.SetTop(children[i], row * cellHeight);
            }
        }
    }
}

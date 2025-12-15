// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Controls.Elements.Containers
{
    /// <summary>
    /// Handler for Grid container elements
    /// </summary>
    public class GridHandler : ElementHandlerBase
    {
        public override string ElementType => "Grid";
        public override double DefaultWidth => 200;
        public override double DefaultHeight => 200;

        private const string GridLinesKey = "GridLines";

        public override void InitializeVisual(UIElementVisual visual, UIElementViewModel viewModel)
        {
            base.InitializeVisual(visual, viewModel);
            UpdateGridLines(visual, viewModel);
        }

        public override void UpdateContent(UIElementVisual visual, UIElementViewModel viewModel)
        {
            UpdateGridLines(visual, viewModel);
        }

        public override void UpdateSize(UIElementVisual visual, UIElementViewModel viewModel)
        {
            UpdateGridLines(visual, viewModel);
        }

        public override bool HandlePropertyChange(UIElementVisual visual, UIElementViewModel viewModel, string propertyName)
        {
            if (propertyName == nameof(UIElementViewModel.RowDefinitions) ||
                propertyName == nameof(UIElementViewModel.ColumnDefinitions) ||
                propertyName == nameof(UIElementViewModel.RowSpacing) ||
                propertyName == nameof(UIElementViewModel.ColumnSpacing))
            {
                UpdateGridLines(visual, viewModel);
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
            yield return "Grid";
        }

        public override void LayoutChildren(UIElementVisual visual, UIElementViewModel viewModel)
        {
            var rows = ParseDefinitions(viewModel.RowDefinitions);
            var cols = ParseDefinitions(viewModel.ColumnDefinitions);

            if (rows.Count == 0) rows.Add(1.0);
            if (cols.Count == 0) cols.Add(1.0);

            var rowHeights = CalculateSizes(rows, viewModel.Height, viewModel.RowSpacing);
            var colWidths = CalculateSizes(cols, viewModel.Width, viewModel.ColumnSpacing);

            foreach (var childVisual in visual.ChildContainer.Children.OfType<UIElementVisual>())
            {
                var childVm = childVisual.ViewModel;
                int row = Math.Clamp(childVm.GridRow, 0, rows.Count - 1);
                int col = Math.Clamp(childVm.GridColumn, 0, cols.Count - 1);

                double x = colWidths.Take(col).Sum() + col * viewModel.ColumnSpacing;
                double y = rowHeights.Take(row).Sum() + row * viewModel.RowSpacing;

                Canvas.SetLeft(childVisual, x);
                Canvas.SetTop(childVisual, y);
            }
        }

        private void UpdateGridLines(UIElementVisual visual, UIElementViewModel viewModel)
        {
            // Remove existing grid lines
            var existing = visual.Children.OfType<Grid>()
                .FirstOrDefault(g => g.Tag as string == GridLinesKey);
            if (existing != null)
                visual.Children.Remove(existing);

            var rows = ParseDefinitions(viewModel.RowDefinitions);
            var cols = ParseDefinitions(viewModel.ColumnDefinitions);

            if (rows.Count <= 1 && cols.Count <= 1)
                return;

            var gridVisual = new Grid
            {
                Width = viewModel.Width,
                Height = viewModel.Height,
                IsHitTestVisible = false,
                Tag = GridLinesKey
            };

            var rowHeights = CalculateSizes(rows, viewModel.Height, viewModel.RowSpacing);
            var colWidths = CalculateSizes(cols, viewModel.Width, viewModel.ColumnSpacing);

            // Draw horizontal lines
            double y = 0;
            for (int i = 0; i < rowHeights.Count - 1; i++)
            {
                y += rowHeights[i] + viewModel.RowSpacing / 2;
                var line = new Line
                {
                    X1 = 0,
                    X2 = viewModel.Width,
                    Y1 = y,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(60, 100, 150, 255)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                gridVisual.Children.Add(line);
                y += viewModel.RowSpacing / 2;
            }

            // Draw vertical lines
            double x = 0;
            for (int i = 0; i < colWidths.Count - 1; i++)
            {
                x += colWidths[i] + viewModel.ColumnSpacing / 2;
                var line = new Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = 0,
                    Y2 = viewModel.Height,
                    Stroke = new SolidColorBrush(Color.FromArgb(60, 100, 150, 255)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                gridVisual.Children.Add(line);
                x += viewModel.ColumnSpacing / 2;
            }

            visual.Children.Insert(1, gridVisual);
        }

        private static List<double> ParseDefinitions(string definitions)
        {
            var result = new List<double>();
            if (string.IsNullOrWhiteSpace(definitions))
                return result;

            foreach (var part in definitions.Split(','))
            {
                var trimmed = part.Trim().TrimEnd('*');
                if (double.TryParse(trimmed, out double value))
                    result.Add(value);
                else
                    result.Add(1.0);
            }
            return result;
        }

        private static List<double> CalculateSizes(List<double> definitions, double totalSize, double spacing)
        {
            double totalSpacing = (definitions.Count - 1) * spacing;
            double available = totalSize - totalSpacing;
            double totalStars = definitions.Sum();

            return definitions.Select(d => (d / totalStars) * available).ToList();
        }
    }
}

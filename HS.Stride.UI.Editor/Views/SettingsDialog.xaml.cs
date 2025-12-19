// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HS.Stride.UI.Editor.Views
{
    /// <summary>
    /// Settings dialog for editor visual customization
    /// </summary>
    public partial class SettingsDialog : Window
    {
        public double DesignWidth { get; set; } = 1280;
        public double DesignHeight { get; set; } = 720;
        public bool ShowGrid { get; set; } = true;
        public Color GuideColor { get; set; } = Color.FromRgb(0, 150, 255);
        public double GuideThickness { get; set; } = 1;
        public Color SelectionColor { get; set; } = Colors.Blue;
        public double SelectionThickness { get; set; } = 2;
        public Color HighlightColor { get; set; } = Color.FromRgb(100, 150, 255);

        public SettingsDialog()
        {
            InitializeComponent();
            // Load settings in Loaded event so caller can set properties first
            Loaded += (s, e) => LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            DesignWidthTextBox.Text = DesignWidth.ToString();
            DesignHeightTextBox.Text = DesignHeight.ToString();
            ShowGridCheckBox.IsChecked = ShowGrid;
            GuideColorPreview.Background = new SolidColorBrush(GuideColor);
            GuideThicknessTextBox.Text = GuideThickness.ToString();
            SelectionColorPreview.Background = new SolidColorBrush(SelectionColor);
            SelectionThicknessTextBox.Text = SelectionThickness.ToString();
            HighlightColorPreview.Background = new SolidColorBrush(HighlightColor);
        }

        private void GuideColorPreview_Click(object sender, MouseButtonEventArgs e)
        {
            var color = ShowColorPicker(GuideColor);
            if (color.HasValue)
            {
                GuideColor = color.Value;
                GuideColorPreview.Background = new SolidColorBrush(GuideColor);
            }
        }

        private void SelectionColorPreview_Click(object sender, MouseButtonEventArgs e)
        {
            var color = ShowColorPicker(SelectionColor);
            if (color.HasValue)
            {
                SelectionColor = color.Value;
                SelectionColorPreview.Background = new SolidColorBrush(SelectionColor);
            }
        }

        private void HighlightColorPreview_Click(object sender, MouseButtonEventArgs e)
        {
            var color = ShowColorPicker(HighlightColor);
            if (color.HasValue)
            {
                HighlightColor = color.Value;
                HighlightColorPreview.Background = new SolidColorBrush(HighlightColor);
            }
        }

        private Color? ShowColorPicker(Color currentColor)
        {
            var dialog = new ColorPickerDialog(currentColor);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                return dialog.SelectedColor;
            }
            return null;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ShowGrid = true;
            GuideColor = Color.FromRgb(0, 150, 255);
            GuideThickness = 1;
            SelectionColor = Colors.Blue;
            SelectionThickness = 2;
            HighlightColor = Color.FromRgb(100, 150, 255);
            LoadCurrentSettings();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Parse design resolution
            if (double.TryParse(DesignWidthTextBox.Text, out double designWidth))
            {
                DesignWidth = Math.Max(100, Math.Min(7680, designWidth)); // Min 100, max 8K
            }

            if (double.TryParse(DesignHeightTextBox.Text, out double designHeight))
            {
                DesignHeight = Math.Max(100, Math.Min(4320, designHeight)); // Min 100, max 8K
            }

            // Get checkbox value
            ShowGrid = ShowGridCheckBox.IsChecked ?? true;

            // Parse thickness values
            if (double.TryParse(GuideThicknessTextBox.Text, out double guideThickness))
            {
                GuideThickness = Math.Max(0.5, Math.Min(5, guideThickness));
            }

            if (double.TryParse(SelectionThicknessTextBox.Text, out double selectionThickness))
            {
                SelectionThickness = Math.Max(1, Math.Min(5, selectionThickness));
            }

            DialogResult = true;
            Close();
        }
    }
}

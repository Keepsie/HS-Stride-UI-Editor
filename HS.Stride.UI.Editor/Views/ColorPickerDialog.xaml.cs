// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HS.Stride.UI.Editor.Views
{
    public partial class ColorPickerDialog : Window
    {
        public Color SelectedColor { get; private set; }

        // HSV values (0-1 range)
        private double _hue;        // 0-360
        private double _saturation; // 0-1
        private double _brightness; // 0-1 (value)
        private byte _alpha = 255;

        // Drag state
        private bool _isDraggingColorSquare;
        private bool _isDraggingHueBar;
        private bool _isDraggingAlphaBar;
        private bool _isUpdating;

        public ColorPickerDialog(Color initialColor)
        {
            InitializeComponent();

            // Convert initial color to HSV
            RgbToHsv(initialColor.R, initialColor.G, initialColor.B, out _hue, out _saturation, out _brightness);
            _alpha = initialColor.A;

            // Update all UI
            UpdateColorSquareBase();
            UpdateColorSelectorPosition();
            UpdateHueSelectorPosition();
            UpdateAlphaSelectorPosition();
            UpdateAlphaGradient();
            UpdatePreview();
            UpdateTextInputs();
        }

        #region HSV Conversion

        private void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;

            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            // Value
            v = max;

            // Saturation
            if (max == 0)
                s = 0;
            else
                s = delta / max;

            // Hue
            if (delta == 0)
            {
                h = 0;
            }
            else if (max == rd)
            {
                h = 60 * (((gd - bd) / delta) % 6);
            }
            else if (max == gd)
            {
                h = 60 * (((bd - rd) / delta) + 2);
            }
            else
            {
                h = 60 * (((rd - gd) / delta) + 4);
            }

            if (h < 0) h += 360;
        }

        private void HsvToRgb(double h, double s, double v, out byte r, out byte g, out byte b)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = v - c;

            double rd, gd, bd;

            if (h < 60) { rd = c; gd = x; bd = 0; }
            else if (h < 120) { rd = x; gd = c; bd = 0; }
            else if (h < 180) { rd = 0; gd = c; bd = x; }
            else if (h < 240) { rd = 0; gd = x; bd = c; }
            else if (h < 300) { rd = x; gd = 0; bd = c; }
            else { rd = c; gd = 0; bd = x; }

            r = (byte)Math.Round((rd + m) * 255);
            g = (byte)Math.Round((gd + m) * 255);
            b = (byte)Math.Round((bd + m) * 255);
        }

        private Color HueToColor(double hue)
        {
            HsvToRgb(hue, 1, 1, out byte r, out byte g, out byte b);
            return Color.FromRgb(r, g, b);
        }

        #endregion

        #region Color Square (Saturation/Brightness)

        private void ColorSquare_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingColorSquare = true;
            ColorSquare.CaptureMouse();
            UpdateColorFromSquare(e.GetPosition(ColorSquare));
        }

        private void ColorSquare_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingColorSquare)
            {
                UpdateColorFromSquare(e.GetPosition(ColorSquare));
            }
        }

        private void ColorSquare_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingColorSquare = false;
            ColorSquare.ReleaseMouseCapture();
        }

        private void UpdateColorFromSquare(Point position)
        {
            // Clamp to bounds
            double x = Math.Max(0, Math.Min(position.X, 256));
            double y = Math.Max(0, Math.Min(position.Y, 256));

            // X = saturation (0 at left, 1 at right)
            _saturation = x / 256.0;
            // Y = brightness/value (1 at top, 0 at bottom)
            _brightness = 1.0 - (y / 256.0);

            // If alpha is 0 (fully transparent), auto-set to 255 so user can see their color choice
            // This is a UX improvement - users picking a color typically want it visible
            if (_alpha == 0)
            {
                _alpha = 255;
                UpdateAlphaSelectorPosition();
            }

            UpdateColorSelectorPosition();
            UpdatePreview();
            UpdateTextInputs();
            UpdateAlphaGradient();
        }

        private void UpdateColorSelectorPosition()
        {
            double x = _saturation * 256;
            double y = (1.0 - _brightness) * 256;

            // Center the selector circle
            Canvas.SetLeft(ColorSelector, x - 7);
            Canvas.SetTop(ColorSelector, y - 7);
        }

        private void UpdateColorSquareBase()
        {
            // Set the base color of the square based on current hue
            ColorSquareBase.Fill = new SolidColorBrush(HueToColor(_hue));
        }

        #endregion

        #region Hue Bar

        private void HueBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingHueBar = true;
            HueBar.CaptureMouse();
            UpdateHueFromBar(e.GetPosition(HueBar));
        }

        private void HueBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingHueBar)
            {
                UpdateHueFromBar(e.GetPosition(HueBar));
            }
        }

        private void HueBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingHueBar = false;
            HueBar.ReleaseMouseCapture();
        }

        private void UpdateHueFromBar(Point position)
        {
            // Clamp to bounds
            double x = Math.Max(0, Math.Min(position.X, 256));

            // X position maps to hue (0-360)
            _hue = (x / 256.0) * 360.0;

            // If alpha is 0 (fully transparent), auto-set to 255 so user can see their color choice
            if (_alpha == 0)
            {
                _alpha = 255;
                UpdateAlphaSelectorPosition();
            }

            UpdateHueSelectorPosition();
            UpdateColorSquareBase();
            UpdatePreview();
            UpdateTextInputs();
            UpdateAlphaGradient();
        }

        private void UpdateHueSelectorPosition()
        {
            double x = (_hue / 360.0) * 256;
            Canvas.SetLeft(HueSelector, x - 2);
        }

        #endregion

        #region Alpha Bar

        private void AlphaBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingAlphaBar = true;
            AlphaBar.CaptureMouse();
            UpdateAlphaFromBar(e.GetPosition(AlphaBar));
        }

        private void AlphaBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingAlphaBar)
            {
                UpdateAlphaFromBar(e.GetPosition(AlphaBar));
            }
        }

        private void AlphaBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingAlphaBar = false;
            AlphaBar.ReleaseMouseCapture();
        }

        private void UpdateAlphaFromBar(Point position)
        {
            // Clamp to bounds
            double x = Math.Max(0, Math.Min(position.X, 256));

            // X position maps to alpha (0-255)
            _alpha = (byte)Math.Round((x / 256.0) * 255);

            UpdateAlphaSelectorPosition();
            UpdatePreview();
            UpdateTextInputs();
        }

        private void UpdateAlphaSelectorPosition()
        {
            double x = (_alpha / 255.0) * 256;
            Canvas.SetLeft(AlphaSelector, x - 2);
        }

        private void UpdateAlphaGradient()
        {
            // Update the alpha gradient to show current color
            HsvToRgb(_hue, _saturation, _brightness, out byte r, out byte g, out byte b);
            var color = Color.FromRgb(r, g, b);

            var gradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0)
            };
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(0, r, g, b), 0));
            gradient.GradientStops.Add(new GradientStop(color, 1));

            AlphaGradient.Fill = gradient;
        }

        #endregion

        #region Preview and Text Inputs

        private void UpdatePreview()
        {
            HsvToRgb(_hue, _saturation, _brightness, out byte r, out byte g, out byte b);
            SelectedColor = Color.FromArgb(_alpha, r, g, b);
            ColorPreview.Fill = new SolidColorBrush(SelectedColor);
        }

        private void UpdateTextInputs()
        {
            if (_isUpdating) return;
            _isUpdating = true;

            HsvToRgb(_hue, _saturation, _brightness, out byte r, out byte g, out byte b);

            RedText.Text = r.ToString();
            GreenText.Text = g.ToString();
            BlueText.Text = b.ToString();
            AlphaText.Text = _alpha.ToString();

            // Update hex
            if (!HexText.IsFocused)
            {
                HexText.Text = $"#{_alpha:X2}{r:X2}{g:X2}{b:X2}";
            }

            _isUpdating = false;
        }

        private void RGBA_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;

            if (!byte.TryParse(RedText.Text, out byte r)) return;
            if (!byte.TryParse(GreenText.Text, out byte g)) return;
            if (!byte.TryParse(BlueText.Text, out byte b)) return;
            if (!byte.TryParse(AlphaText.Text, out byte a)) return;

            _isUpdating = true;

            // Convert RGB to HSV
            RgbToHsv(r, g, b, out _hue, out _saturation, out _brightness);
            _alpha = a;

            // Update visual elements
            UpdateColorSquareBase();
            UpdateColorSelectorPosition();
            UpdateHueSelectorPosition();
            UpdateAlphaSelectorPosition();
            UpdateAlphaGradient();
            UpdatePreview();

            // Update hex
            if (!HexText.IsFocused)
            {
                HexText.Text = $"#{_alpha:X2}{r:X2}{g:X2}{b:X2}";
            }

            _isUpdating = false;
        }

        private void HexText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyHexValue();
        }

        private void ApplyHexValue()
        {
            if (_isUpdating) return;

            string hex = HexText.Text.TrimStart('#');

            try
            {
                Color color;
                if (hex.Length == 6)
                {
                    // RGB format
                    color = (Color)ColorConverter.ConvertFromString("#FF" + hex);
                }
                else if (hex.Length == 8)
                {
                    // ARGB format
                    color = (Color)ColorConverter.ConvertFromString("#" + hex);
                }
                else
                {
                    return;
                }

                _isUpdating = true;

                // Convert to HSV
                RgbToHsv(color.R, color.G, color.B, out _hue, out _saturation, out _brightness);
                _alpha = color.A;

                // Update all UI
                UpdateColorSquareBase();
                UpdateColorSelectorPosition();
                UpdateHueSelectorPosition();
                UpdateAlphaSelectorPosition();
                UpdateAlphaGradient();
                UpdatePreview();

                // Update RGBA text
                RedText.Text = color.R.ToString();
                GreenText.Text = color.G.ToString();
                BlueText.Text = color.B.ToString();
                AlphaText.Text = color.A.ToString();

                _isUpdating = false;
            }
            catch
            {
                // Invalid hex, ignore
            }
        }

        #endregion

        #region Dialog Buttons

        private void TransparentButton_Click(object sender, RoutedEventArgs e)
        {
            // Set to fully transparent (keeps current RGB but sets alpha to 0)
            _alpha = 0;

            UpdateAlphaSelectorPosition();
            UpdateAlphaGradient();
            UpdatePreview();
            UpdateTextInputs();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        #endregion
    }
}

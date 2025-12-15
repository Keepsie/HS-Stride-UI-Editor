// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Core.Services
{
    /// <summary>
    /// Handles property validation, parsing, and constraints for UI elements
    /// </summary>
    public class PropertyService
    {
        #region Property Constraints

        public const double MinWidth = 10.0;
        public const double MinHeight = 10.0;
        public const double MinFontSize = 1.0;
        public const double MinOpacity = 0.0;
        public const double MaxOpacity = 1.0;

        #endregion

        #region Property Parsing and Validation

        /// <summary>
        /// Parse and validate a string value for a specific property, applying constraints
        /// </summary>
        public bool TryParseAndValidate(string propertyName, string textValue, out object? validatedValue)
        {
            validatedValue = null;

            switch (propertyName)
            {
                case "X":
                case "Y":
                    if (double.TryParse(textValue, out double position))
                    {
                        validatedValue = position;
                        return true;
                    }
                    return false;

                case "Width":
                    if (double.TryParse(textValue, out double width))
                    {
                        validatedValue = Math.Max(MinWidth, width);
                        return true;
                    }
                    return false;

                case "Height":
                    if (double.TryParse(textValue, out double height))
                    {
                        validatedValue = Math.Max(MinHeight, height);
                        return true;
                    }
                    return false;

                case "FontSize":
                    if (double.TryParse(textValue, out double fontSize))
                    {
                        validatedValue = Math.Max(MinFontSize, fontSize);
                        return true;
                    }
                    return false;

                case "Opacity":
                    if (double.TryParse(textValue, out double opacity))
                    {
                        validatedValue = Math.Max(MinOpacity, Math.Min(MaxOpacity, opacity));
                        return true;
                    }
                    return false;

                case "DrawLayerNumber":
                    if (int.TryParse(textValue, out int layer))
                    {
                        validatedValue = layer;
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Apply a parsed and validated value to the element's property
        /// </summary>
        public bool ApplyProperty(UIElementViewModel element, string propertyName, object value)
        {
            try
            {
                switch (propertyName)
                {
                    case "X":
                        element.X = (double)value;
                        return true;

                    case "Y":
                        element.Y = (double)value;
                        return true;

                    case "Width":
                        element.Width = (double)value;
                        return true;

                    case "Height":
                        element.Height = (double)value;
                        return true;

                    case "FontSize":
                        element.FontSize = (double)value;
                        return true;

                    case "Opacity":
                        element.Opacity = (double)value;
                        return true;

                    case "DrawLayerNumber":
                        element.DrawLayerNumber = (int)value;
                        return true;

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate and apply a property value from a text string (convenience method)
        /// </summary>
        public bool ValidateAndApply(UIElementViewModel element, string propertyName, string textValue)
        {
            if (TryParseAndValidate(propertyName, textValue, out object? validatedValue) && validatedValue != null)
            {
                return ApplyProperty(element, propertyName, validatedValue);
            }
            return false;
        }

        #endregion

        #region Property Mapping (for Asset Selection)

        /// <summary>
        /// Map an asset property target name to the actual property setter action
        /// </summary>
        public Action<UIElementViewModel, string>? GetAssetPropertySetter(string target)
        {
            return target switch
            {
                "ImageSource" => (e, v) => e.ImageSource = v,
                "ButtonNotPressed" => (e, v) => e.ButtonNotPressedImage = v,
                "ButtonMouseOver" => (e, v) => e.ButtonMouseOverImage = v,
                "ButtonPressed" => (e, v) => e.ButtonPressedImage = v,
                "EditTextActive" => (e, v) => e.EditTextActiveImage = v,
                "EditTextInactive" => (e, v) => e.EditTextInactiveImage = v,
                "EditTextMouseOver" => (e, v) => e.EditTextMouseOverImage = v,
                "SliderTrackBg" => (e, v) => e.SliderTrackBackgroundImage = v,
                "SliderTrackFg" => (e, v) => e.SliderTrackForegroundImage = v,
                "SliderThumb" => (e, v) => e.SliderThumbImage = v,
                "SliderMouseOverThumb" => (e, v) => e.SliderMouseOverThumbImage = v,
                "SliderTick" => (e, v) => e.SliderTickImage = v,
                "ToggleChecked" => (e, v) => e.ToggleCheckedImage = v,
                "ToggleUnchecked" => (e, v) => e.ToggleUncheckedImage = v,
                "ToggleIndeterminate" => (e, v) => e.ToggleIndeterminateImage = v,
                _ => null
            };
        }

        /// <summary>
        /// Apply an asset selection to the appropriate property
        /// </summary>
        public bool ApplyAssetProperty(UIElementViewModel element, string target, string assetName)
        {
            var setter = GetAssetPropertySetter(target);
            if (setter != null)
            {
                setter(element, assetName);
                return true;
            }
            return false;
        }

        #endregion

        #region Default Values

        /// <summary>
        /// Get default value for a property based on element type
        /// </summary>
        public object? GetDefaultValue(string elementType, string propertyName)
        {
            // Element-type-specific defaults (more specific patterns first)
            return (elementType, propertyName) switch
            {
                ("ImageElement", "Height") => 200.0, // Square for images (specific case first)
                (_, "Width") => 200.0,
                (_, "Height") => 100.0,
                (_, "X") => 0.0,
                (_, "Y") => 0.0,
                (_, "Opacity") => 1.0,
                (_, "FontSize") => 16.0,
                (_, "DrawLayerNumber") => 0,
                _ => null
            };
        }

        #endregion

        #region Property Info

        /// <summary>
        /// Check if a property is numeric
        /// </summary>
        public bool IsNumericProperty(string propertyName)
        {
            return propertyName is "X" or "Y" or "Width" or "Height" or "FontSize" or "Opacity" or "DrawLayerNumber";
        }

        /// <summary>
        /// Get property constraints as a human-readable string
        /// </summary>
        public string GetPropertyConstraints(string propertyName)
        {
            return propertyName switch
            {
                "Width" => $"Min: {MinWidth}",
                "Height" => $"Min: {MinHeight}",
                "FontSize" => $"Min: {MinFontSize}",
                "Opacity" => $"Range: {MinOpacity} - {MaxOpacity}",
                _ => ""
            };
        }

        #endregion
    }
}

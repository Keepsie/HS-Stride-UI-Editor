// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HS.Stride.UI.Editor.Core.Services;
using HS.Stride.UI.Editor.Core.Services.Interfaces;
using HS.Stride.UI.Editor.Models;
using HS.Stride.UI.Editor.ViewModels;
using HS.Stride.Editor.Toolkit.Core.AssetEditing;

namespace HS.Stride.UI.Editor.Views
{
    /// <summary>
    /// Event args for property changes with undo support
    /// </summary>
    public class PropertyChangedUndoEventArgs : EventArgs
    {
        public UIElementViewModel Element { get; }
        public string PropertyName { get; }
        public object? OldValue { get; }
        public object? NewValue { get; }

        public PropertyChangedUndoEventArgs(UIElementViewModel element, string propertyName, object? oldValue, object? newValue)
        {
            Element = element;
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public partial class PropertyPanel : UserControl
    {
        private UIElementViewModel? _currentElement;
        private readonly PropertyService _propertyService = new();
        public Func<List<AssetItem>>? GetProjectAssets { get; set; }

        // Font management
        private IAssetService? _assetService;

        // Flag to suppress property updates during LoadElement
        private bool _isLoading = false;

        // Track property values for undo support
        private Dictionary<string, object?> _propertyStartValues = new();
        private string? _activeEditProperty = null;

        /// <summary>
        /// Event fired when any property is changed via the panel
        /// </summary>
        public event EventHandler? PropertyChanged;

        /// <summary>
        /// Event fired when a property change is committed (for undo support)
        /// </summary>
        public event EventHandler<PropertyChangedUndoEventArgs>? PropertyChangeCommitted;

        public PropertyPanel()
        {
            InitializeComponent();

            // Wire up events for REAL-TIME UPDATES
            SetupRealTimePropertyUpdates();

            // Wire up undo tracking for TextBox controls
            SetupUndoTracking();

            // Wire up events - Opacity slider with undo support
            double? opacityStart = null;
            PropOpacity.GotFocus += (s, e) => opacityStart = _currentElement?.Opacity;
            PropOpacity.ValueChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.Opacity = PropOpacity.Value;
                    PropOpacityText.Text = $"{(int)(PropOpacity.Value * 100)}%";
                    NotifyPropertyChanged();
                }
            };
            PropOpacity.LostFocus += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && opacityStart.HasValue)
                {
                    FireImmediatePropertyChange("Opacity", opacityStart.Value, _currentElement.Opacity);
                    opacityStart = null;
                }
            };

            // Font families will be populated when SetAssetService is called

            // Color picker buttons
            PropBgColorButton.Click += PropBgColorButton_Click;
            PropImageTintButton.Click += PropImageTintButton_Click;
            PropTextColorButton.Click += PropTextColorButton_Click;
            PropTextOutlineColorButton.Click += PropTextOutlineColorButton_Click;
            PropCaretColorButton.Click += PropCaretColorButton_Click;
            PropSelectionColorButton.Click += PropSelectionColorButton_Click;
            PropIMESelectionColorButton.Click += PropIMESelectionColorButton_Click;
            PropScrollBarColorButton.Click += PropScrollBarColorButton_Click;
            PropOverlayColorButton.Click += PropOverlayColorButton_Click;
            PropBorderColorButton.Click += PropBorderColorButton_Click;

            // Asset picker buttons - ImageElement
            PropImageBrowse.Click += (s, e) => BrowseForAsset("Image", "ImageSource");

            // Asset picker buttons - Button states
            PropButtonNotPressedBrowse.Click += (s, e) => BrowseForAsset("Image", "ButtonNotPressed");
            PropButtonMouseOverBrowse.Click += (s, e) => BrowseForAsset("Image", "ButtonMouseOver");
            PropButtonPressedBrowse.Click += (s, e) => BrowseForAsset("Image", "ButtonPressed");

            // Asset picker buttons - EditText states
            PropEditTextActiveBrowse.Click += (s, e) => BrowseForAsset("Image", "EditTextActive");
            PropEditTextInactiveBrowse.Click += (s, e) => BrowseForAsset("Image", "EditTextInactive");
            PropEditTextMouseOverBrowse.Click += (s, e) => BrowseForAsset("Image", "EditTextMouseOver");

            // Asset picker buttons - Slider images
            PropSliderTrackBgBrowse.Click += (s, e) => BrowseForAsset("Image", "SliderTrackBg");
            PropSliderTrackFgBrowse.Click += (s, e) => BrowseForAsset("Image", "SliderTrackFg");
            PropSliderThumbBrowse.Click += (s, e) => BrowseForAsset("Image", "SliderThumb");
            PropSliderMouseOverThumbBrowse.Click += (s, e) => BrowseForAsset("Image", "SliderMouseOverThumb");
            PropSliderTickBrowse.Click += (s, e) => BrowseForAsset("Image", "SliderTick");

            // Asset picker buttons - ToggleButton states
            PropToggleCheckedBrowse.Click += (s, e) => BrowseForAsset("Image", "ToggleChecked");
            PropToggleUncheckedBrowse.Click += (s, e) => BrowseForAsset("Image", "ToggleUnchecked");
            PropToggleIndeterminateBrowse.Click += (s, e) => BrowseForAsset("Image", "ToggleIndeterminate");

            // Asset picker buttons - ContentDecorator background image
            PropBackgroundImageBrowse.Click += (s, e) => BrowseForAsset("Image", "BackgroundImage");
        }

        /// <summary>
        /// Sets up undo tracking for all TextBox controls
        /// </summary>
        private void SetupUndoTracking()
        {
            // Layout properties
            SetupTextBoxUndoTracking(PropX, "X", () => _currentElement?.X);
            SetupTextBoxUndoTracking(PropY, "Y", () => _currentElement?.Y);
            SetupTextBoxUndoTracking(PropWidth, "Width", () => _currentElement?.Width);
            SetupTextBoxUndoTracking(PropHeight, "Height", () => _currentElement?.Height);
            SetupTextBoxUndoTracking(PropZIndex, "ZIndex", () => _currentElement?.ZIndex);
            SetupTextBoxUndoTracking(PropDrawLayer, "DrawLayerNumber", () => _currentElement?.DrawLayerNumber);

            // Text properties
            SetupTextBoxUndoTracking(PropText, "Text", () => _currentElement?.Text);
            SetupTextBoxUndoTracking(PropFontSize, "FontSize", () => _currentElement?.FontSize);
            SetupTextBoxUndoTracking(PropButtonText, "ButtonText", () => _currentElement?.ButtonText);
            SetupTextBoxUndoTracking(PropTextOutlineThickness, "TextOutlineThickness", () => _currentElement?.TextOutlineThickness);

            // Image properties
            SetupTextBoxUndoTracking(PropSpriteFrame, "SpriteFrame", () => _currentElement?.SpriteFrame);

            // Button sprite frames
            SetupTextBoxUndoTracking(PropButtonNotPressedFrame, "ButtonNotPressedFrame", () => _currentElement?.ButtonNotPressedFrame);
            SetupTextBoxUndoTracking(PropButtonMouseOverFrame, "ButtonMouseOverFrame", () => _currentElement?.ButtonMouseOverFrame);
            SetupTextBoxUndoTracking(PropButtonPressedFrame, "ButtonPressedFrame", () => _currentElement?.ButtonPressedFrame);

            // Slider properties
            SetupTextBoxUndoTracking(PropSliderMinimum, "SliderMinimum", () => _currentElement?.SliderMinimum);
            SetupTextBoxUndoTracking(PropSliderMaximum, "SliderMaximum", () => _currentElement?.SliderMaximum);
            SetupTextBoxUndoTracking(PropSliderValue, "SliderValue", () => _currentElement?.SliderValue);
            SetupTextBoxUndoTracking(PropSliderStep, "SliderStep", () => _currentElement?.SliderStep);
            SetupTextBoxUndoTracking(PropSliderTickFrequency, "SliderTickFrequency", () => _currentElement?.SliderTickFrequency);
            SetupTextBoxUndoTracking(PropSliderTickOffset, "SliderTickOffset", () => _currentElement?.SliderTickOffset);

            // ScrollViewer properties
            SetupTextBoxUndoTracking(PropScrollBarThickness, "ScrollBarThickness", () => _currentElement?.ScrollBarThickness);
            SetupTextBoxUndoTracking(PropScrollingSpeed, "ScrollingSpeed", () => _currentElement?.ScrollingSpeed);
            SetupTextBoxUndoTracking(PropDeceleration, "Deceleration", () => _currentElement?.Deceleration);
            SetupTextBoxUndoTracking(PropScrollStartThreshold, "ScrollStartThreshold", () => _currentElement?.ScrollStartThreshold);

            // EditText properties
            SetupTextBoxUndoTracking(PropMaxLength, "MaxLength", () => _currentElement?.MaxLength);
            SetupTextBoxUndoTracking(PropMinLines, "MinLines", () => _currentElement?.MinLines);
            SetupTextBoxUndoTracking(PropMaxLines, "MaxLines", () => _currentElement?.MaxLines);
            SetupTextBoxUndoTracking(PropCaretWidth, "CaretWidth", () => _currentElement?.CaretWidth);
            SetupTextBoxUndoTracking(PropCaretFrequency, "CaretFrequency", () => _currentElement?.CaretFrequency);
        }

        private void SetupRealTimePropertyUpdates()
        {
            // Lock toggle - real-time update with undo
            PropIsLocked.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    var oldValue = !(_currentElement.IsLocked); // Click toggles, so old is opposite
                    _currentElement.IsLocked = PropIsLocked.IsChecked == true;
                    NotifyPropertyChanged();
                    FireImmediatePropertyChange("IsLocked", oldValue, _currentElement.IsLocked);
                }
            };

            // Allow overflow toggle - real-time update with undo
            PropAllowOverflow.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    var oldValue = !(_currentElement.AllowCanvasOverflow);
                    _currentElement.AllowCanvasOverflow = PropAllowOverflow.IsChecked == true;
                    NotifyPropertyChanged();
                    FireImmediatePropertyChange("AllowCanvasOverflow", oldValue, _currentElement.AllowCanvasOverflow);
                }
            };

            // Position properties - update on text change
            PropX.TextChanged += (s, e) => UpdateRealTimeProperty("X", PropX.Text);
            PropY.TextChanged += (s, e) => UpdateRealTimeProperty("Y", PropY.Text);
            PropWidth.TextChanged += (s, e) => UpdateRealTimeProperty("Width", PropWidth.Text);
            PropHeight.TextChanged += (s, e) => UpdateRealTimeProperty("Height", PropHeight.Text);
            PropZIndex.TextChanged += (s, e) => UpdateRealTimeProperty("ZIndex", PropZIndex.Text);
            PropDrawLayer.TextChanged += (s, e) => UpdateRealTimeProperty("DrawLayerNumber", PropDrawLayer.Text);

            // Text properties
            PropText.TextChanged += (s, e) => { if (_currentElement != null && !_isLoading) { _currentElement.Text = PropText.Text; NotifyPropertyChanged(); } };
            PropFontSize.TextChanged += (s, e) => UpdateRealTimeProperty("FontSize", PropFontSize.Text);

            // Button text
            PropButtonText.TextChanged += (s, e) => { if (_currentElement != null && !_isLoading) { _currentElement.ButtonText = PropButtonText.Text; NotifyPropertyChanged(); } };

            // Combo boxes - alignment changes trigger position recalculation in ViewModel (with undo)
            PropHAlign.SelectionChanged += (s, e) => {
                if (_currentElement != null && !_isLoading)
                {
                    var oldValue = _currentElement.HorizontalAlignment;
                    _currentElement.HorizontalAlignment = ((ComboBoxItem)PropHAlign.SelectedItem)?.Content.ToString() ?? "Stretch";
                    NotifyPropertyChanged();
                    FireImmediatePropertyChange("HorizontalAlignment", oldValue, _currentElement.HorizontalAlignment);
                }
            };
            PropVAlign.SelectionChanged += (s, e) => {
                if (_currentElement != null && !_isLoading)
                {
                    var oldValue = _currentElement.VerticalAlignment;
                    _currentElement.VerticalAlignment = ((ComboBoxItem)PropVAlign.SelectedItem)?.Content.ToString() ?? "Stretch";
                    NotifyPropertyChanged();
                    FireImmediatePropertyChange("VerticalAlignment", oldValue, _currentElement.VerticalAlignment);
                }
            };

            // Text alignment - real-time update with undo
            PropTextAlign.SelectionChanged += (s, e) => {
                if (_currentElement != null && !_isLoading)
                {
                    var oldValue = _currentElement.TextAlignment;
                    _currentElement.TextAlignment = GetComboBoxValue(PropTextAlign);
                    NotifyPropertyChanged();
                    FireImmediatePropertyChange("TextAlignment", oldValue, _currentElement.TextAlignment);
                }
            };

            // Text outline thickness - real-time update
            PropTextOutlineThickness.TextChanged += (s, e) => { if (_currentElement != null && !_isLoading && double.TryParse(PropTextOutlineThickness.Text, out double val)) { _currentElement.TextOutlineThickness = val; NotifyPropertyChanged(); } };

            // Font browse button
            PropFontBrowse.Click += (s, e) => BrowseForAsset("Font", "Font");

            // Image properties - sprite frame updates canvas
            PropSpriteFrame.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && int.TryParse(PropSpriteFrame.Text, out int frame))
                {
                    _currentElement.SpriteFrame = Math.Max(0, frame);
                    NotifyPropertyChanged();
                }
            };

            // Image source mode - clear image when changing type (with undo)
            PropImageSourceMode.SelectionChanged += (s, e) =>
            {
                if (_currentElement == null || _isLoading) return;
                var newMode = GetComboBoxValue(PropImageSourceMode);
                var oldMode = _currentElement.ImageAssetType;

                // If mode changed and we have an image, clear it (incompatible types)
                if (newMode != oldMode && !string.IsNullOrEmpty(_currentElement.ImageSource))
                {
                    _currentElement.ImageSource = "";
                    _currentElement.ImageAssetReference = null;
                    _currentElement.SpriteFrame = 0;
                    PropImageSource.Text = "";
                    PropSpriteFrame.Text = "0";
                }
                _currentElement.ImageAssetType = newMode;
                NotifyPropertyChanged();
                FireImmediatePropertyChange("ImageAssetType", oldMode, newMode);
            };

            // Button image mode - clear images when changing type (with undo)
            PropButtonImageMode.SelectionChanged += (s, e) =>
            {
                if (_currentElement == null || _isLoading) return;
                var newMode = GetComboBoxValue(PropButtonImageMode);
                var oldMode = _currentElement.ButtonImageMode;

                if (newMode != oldMode)
                {
                    // Clear all button images
                    ClearButtonImages();
                }
                _currentElement.ButtonImageMode = newMode;
                NotifyPropertyChanged();
                FireImmediatePropertyChange("ButtonImageMode", oldMode, newMode);
            };

            // EditText image mode - clear images when changing type (with undo)
            PropEditTextImageMode.SelectionChanged += (s, e) =>
            {
                if (_currentElement == null || _isLoading) return;
                var newMode = GetComboBoxValue(PropEditTextImageMode);
                var oldMode = _currentElement.EditTextImageMode;

                if (newMode != oldMode)
                {
                    ClearEditTextImages();
                }
                _currentElement.EditTextImageMode = newMode;
                NotifyPropertyChanged();
                FireImmediatePropertyChange("EditTextImageMode", oldMode, newMode);
            };

            // Button sprite frames
            PropButtonNotPressedFrame.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && int.TryParse(PropButtonNotPressedFrame.Text, out int frame))
                {
                    _currentElement.ButtonNotPressedFrame = Math.Max(0, frame);
                    NotifyPropertyChanged();
                }
            };
            PropButtonMouseOverFrame.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && int.TryParse(PropButtonMouseOverFrame.Text, out int frame))
                {
                    _currentElement.ButtonMouseOverFrame = Math.Max(0, frame);
                    NotifyPropertyChanged();
                }
            };
            PropButtonPressedFrame.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && int.TryParse(PropButtonPressedFrame.Text, out int frame))
                {
                    _currentElement.ButtonPressedFrame = Math.Max(0, frame);
                    NotifyPropertyChanged();
                }
            };

            // Slider properties - real-time updates
            PropSliderMinimum.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && double.TryParse(PropSliderMinimum.Text, out double val))
                { _currentElement.SliderMinimum = val; NotifyPropertyChanged(); }
            };
            PropSliderMaximum.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && double.TryParse(PropSliderMaximum.Text, out double val))
                { _currentElement.SliderMaximum = val; NotifyPropertyChanged(); }
            };
            PropSliderValue.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && double.TryParse(PropSliderValue.Text, out double val))
                { _currentElement.SliderValue = val; NotifyPropertyChanged(); }
            };
            PropSliderStep.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && double.TryParse(PropSliderStep.Text, out double val))
                { _currentElement.SliderStep = Math.Max(0, val); NotifyPropertyChanged(); }
            };
            PropSliderTickFrequency.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && double.TryParse(PropSliderTickFrequency.Text, out double val))
                { _currentElement.SliderTickFrequency = Math.Max(0, val); NotifyPropertyChanged(); }
            };
            PropSliderTickOffset.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && double.TryParse(PropSliderTickOffset.Text, out double val))
                { _currentElement.SliderTickOffset = Math.Max(0, val); NotifyPropertyChanged(); }
            };

            // ScrollViewer properties - real-time updates
            PropScrollBarThickness.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && double.TryParse(PropScrollBarThickness.Text, out double val))
                { _currentElement.ScrollBarThickness = Math.Max(1, val); NotifyPropertyChanged(); }
            };
            PropScrollingSpeed.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && double.TryParse(PropScrollingSpeed.Text, out double val))
                { _currentElement.ScrollingSpeed = Math.Max(0, val); NotifyPropertyChanged(); }
            };
            PropDeceleration.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && double.TryParse(PropDeceleration.Text, out double val))
                { _currentElement.Deceleration = Math.Max(0, val); NotifyPropertyChanged(); }
            };
            PropScrollStartThreshold.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && double.TryParse(PropScrollStartThreshold.Text, out double val))
                { _currentElement.ScrollStartThreshold = Math.Max(0, val); NotifyPropertyChanged(); }
            };

            // EditText properties - real-time updates
            PropMaxLength.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && int.TryParse(PropMaxLength.Text, out int val))
                { _currentElement.MaxLength = Math.Max(0, val); NotifyPropertyChanged(); }
            };
            PropMinLines.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && int.TryParse(PropMinLines.Text, out int val))
                { _currentElement.MinLines = Math.Max(1, val); NotifyPropertyChanged(); }
            };
            PropMaxLines.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && int.TryParse(PropMaxLines.Text, out int val))
                { _currentElement.MaxLines = Math.Max(1, val); NotifyPropertyChanged(); }
            };
            PropCaretWidth.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && double.TryParse(PropCaretWidth.Text, out double val))
                { _currentElement.CaretWidth = Math.Max(0, val); NotifyPropertyChanged(); }
            };
            PropCaretFrequency.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading && double.TryParse(PropCaretFrequency.Text, out double val))
                { _currentElement.CaretFrequency = Math.Max(0, val); NotifyPropertyChanged(); }
            };

            // ToggleButton state - real-time update with undo
            PropToggleState.SelectionChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    var oldValue = _currentElement.ToggleState;
                    _currentElement.ToggleState = GetComboBoxValue(PropToggleState);
                    NotifyPropertyChanged();
                    FireImmediatePropertyChange("ToggleState", oldValue, _currentElement.ToggleState);
                }
            };
            PropToggleClickMode.SelectionChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    var oldValue = _currentElement.ToggleClickMode;
                    _currentElement.ToggleClickMode = GetComboBoxValue(PropToggleClickMode);
                    NotifyPropertyChanged();
                    FireImmediatePropertyChange("ToggleClickMode", oldValue, _currentElement.ToggleClickMode);
                }
            };

            // === Additional real-time handlers (removing need for Apply button) ===

            // Name
            PropName.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.Name = PropName.Text;
                    NotifyPropertyChanged();
                }
            };

            // Appearance - ClipToBounds
            PropClipToBounds.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.ClipToBounds = PropClipToBounds.IsChecked == true;
                    NotifyPropertyChanged();
                }
            };

            // Behavior - Visibility
            PropVisibility.SelectionChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.Visibility = GetComboBoxValue(PropVisibility);
                    NotifyPropertyChanged();
                }
            };

            // Behavior - IsEnabled
            PropIsEnabled.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.IsEnabled = PropIsEnabled.IsChecked == true;
                    NotifyPropertyChanged();
                }
            };

            // Behavior - CanBeHitByUser
            PropCanBeHitByUser.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.CanBeHitByUser = PropCanBeHitByUser.IsChecked == true;
                    NotifyPropertyChanged();
                }
            };

            // TextBlock - WrapText
            PropWrapText.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.WrapText = PropWrapText.IsChecked == true;
                    NotifyPropertyChanged();
                }
            };

            // TextBlock - DoNotSnapText
            PropDoNotSnapText.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.DoNotSnapText = PropDoNotSnapText.IsChecked == true;
                    NotifyPropertyChanged();
                }
            };

            // EditText - Placeholder text
            PropEditTextPlaceholder.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.Text = PropEditTextPlaceholder.Text;
                    NotifyPropertyChanged();
                }
            };

            // EditText - IsReadOnly
            PropIsReadOnly.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.IsReadOnly = PropIsReadOnly.IsChecked == true;
                    NotifyPropertyChanged();
                }
            };

            // EditText - InputType
            PropInputType.SelectionChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.InputType = GetComboBoxValue(PropInputType);
                    NotifyPropertyChanged();
                }
            };

            // Button - ClickMode
            PropClickMode.SelectionChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.ClickMode = GetComboBoxValue(PropClickMode);
                    NotifyPropertyChanged();
                }
            };

            // Slider - AreTicksDisplayed
            PropAreTicksDisplayed.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.AreTicksDisplayed = PropAreTicksDisplayed.IsChecked == true;
                    NotifyPropertyChanged();
                }
            };

            // Slider - ShouldSnapToTicks
            PropShouldSnapToTicks.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.ShouldSnapToTicks = PropShouldSnapToTicks.IsChecked == true;
                    NotifyPropertyChanged();
                }
            };

            // Slider - IsDirectionReversed
            PropIsDirectionReversed.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.IsDirectionReversed = PropIsDirectionReversed.IsChecked == true;
                    NotifyPropertyChanged();
                }
            };

            // ScrollViewer - ScrollMode
            PropScrollMode.SelectionChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.ScrollMode = GetComboBoxValue(PropScrollMode);
                    NotifyPropertyChanged();
                }
            };

            // ScrollViewer - TouchScrollingEnabled
            PropTouchScrollingEnabled.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.TouchScrollingEnabled = PropTouchScrollingEnabled.IsChecked == true;
                    NotifyPropertyChanged();
                }
            };

            // ScrollViewer - SnapToAnchors
            PropSnapToAnchors.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.SnapToAnchors = PropSnapToAnchors.IsChecked == true;
                    NotifyPropertyChanged();
                }
            };

            // Modal - IsModal
            PropIsModal.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.IsModal = PropIsModal.IsChecked == true;
                    NotifyPropertyChanged();
                }
            };

            // ToggleButton - IsThreeState
            PropIsThreeState.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.IsThreeState = PropIsThreeState.IsChecked == true;
                    NotifyPropertyChanged();
                }
            };

            // StackPanel - ItemVirtualizationEnabled
            PropItemVirtualizationEnabled.Click += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.ItemVirtualizationEnabled = PropItemVirtualizationEnabled.IsChecked == true;
                    NotifyPropertyChanged();
                }
            };

            // StackPanel - Orientation
            PropStackPanelOrientation.SelectionChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.StackPanelOrientation = GetComboBoxValue(PropStackPanelOrientation);
                    NotifyPropertyChanged();
                }
            };

            // Slider - Orientation
            PropSliderOrientation.SelectionChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.SliderOrientation = GetComboBoxValue(PropSliderOrientation);
                    NotifyPropertyChanged();
                }
            };

            // Slider - ImageMode
            PropSliderImageMode.SelectionChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.SliderImageMode = GetComboBoxValue(PropSliderImageMode);
                    NotifyPropertyChanged();
                }
            };

            // ToggleButton - ImageMode
            PropToggleImageMode.SelectionChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.ToggleImageMode = GetComboBoxValue(PropToggleImageMode);
                    NotifyPropertyChanged();
                }
            };

            // ToggleButton - Text
            PropToggleButtonText.TextChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.ButtonText = PropToggleButtonText.Text;
                    NotifyPropertyChanged();
                }
            };

            // ContentDecorator - ImageMode
            PropContentDecoratorImageMode.SelectionChanged += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    _currentElement.ContentDecoratorImageMode = GetComboBoxValue(PropContentDecoratorImageMode);
                    NotifyPropertyChanged();
                }
            };
        }

        private void ClearButtonImages()
        {
            if (_currentElement == null) return;
            _currentElement.ButtonNotPressedImage = "";
            _currentElement.ButtonNotPressedImageAsset = null;
            _currentElement.ButtonNotPressedFrame = 0;
            _currentElement.ButtonMouseOverImage = "";
            _currentElement.ButtonMouseOverImageAsset = null;
            _currentElement.ButtonMouseOverFrame = 0;
            _currentElement.ButtonPressedImage = "";
            _currentElement.ButtonPressedImageAsset = null;
            _currentElement.ButtonPressedFrame = 0;

            PropButtonNotPressedImage.Text = "";
            PropButtonNotPressedFrame.Text = "0";
            PropButtonMouseOverImage.Text = "";
            PropButtonMouseOverFrame.Text = "0";
            PropButtonPressedImage.Text = "";
            PropButtonPressedFrame.Text = "0";
        }

        private void ClearEditTextImages()
        {
            if (_currentElement == null) return;
            _currentElement.EditTextActiveImage = "";
            _currentElement.EditTextActiveImageAsset = null;
            _currentElement.EditTextActiveFrame = 0;
            _currentElement.EditTextInactiveImage = "";
            _currentElement.EditTextInactiveImageAsset = null;
            _currentElement.EditTextInactiveFrame = 0;
            _currentElement.EditTextMouseOverImage = "";
            _currentElement.EditTextMouseOverImageAsset = null;
            _currentElement.EditTextMouseOverFrame = 0;

            PropEditTextActiveImage.Text = "";
            PropEditTextActiveFrame.Text = "0";
            PropEditTextInactiveImage.Text = "";
            PropEditTextInactiveFrame.Text = "0";
            PropEditTextMouseOverImage.Text = "";
            PropEditTextMouseOverFrame.Text = "0";
        }

        private void UpdateRealTimeProperty(string propertyName, string textValue)
        {
            if (_currentElement == null || _isLoading) return;
            _propertyService.ValidateAndApply(_currentElement, propertyName, textValue);
            NotifyPropertyChanged();
        }

        /// <summary>
        /// Notify that a property has been changed (for unsaved changes tracking)
        /// </summary>
        private void NotifyPropertyChanged()
        {
            if (!_isLoading)
            {
                PropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Records the start value of a property when editing begins
        /// </summary>
        private void BeginPropertyEdit(string propertyName, object? currentValue)
        {
            if (_currentElement == null || _isLoading) return;
            _activeEditProperty = propertyName;
            _propertyStartValues[propertyName] = currentValue;
        }

        /// <summary>
        /// Commits a property edit and fires undo event if value changed
        /// </summary>
        private void CommitPropertyEdit(string propertyName, object? newValue)
        {
            if (_currentElement == null || _isLoading) return;

            if (_propertyStartValues.TryGetValue(propertyName, out var oldValue))
            {
                // Only fire event if value actually changed
                if (!Equals(oldValue, newValue))
                {
                    PropertyChangeCommitted?.Invoke(this, new PropertyChangedUndoEventArgs(
                        _currentElement, propertyName, oldValue, newValue));
                }
                _propertyStartValues.Remove(propertyName);
            }
            _activeEditProperty = null;
        }

        /// <summary>
        /// Wire up focus events for a TextBox to track edits
        /// </summary>
        private void SetupTextBoxUndoTracking(TextBox textBox, string propertyName, Func<object?> getValue)
        {
            textBox.GotFocus += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    BeginPropertyEdit(propertyName, getValue());
                }
            };

            textBox.LostFocus += (s, e) =>
            {
                if (_currentElement != null && !_isLoading)
                {
                    CommitPropertyEdit(propertyName, getValue());
                }
            };

            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter && _currentElement != null && !_isLoading)
                {
                    CommitPropertyEdit(propertyName, getValue());
                    // Move focus away to prevent duplicate commits
                    Keyboard.ClearFocus();
                }
            };
        }

        /// <summary>
        /// Fire immediate property change (for controls that don't have focus tracking)
        /// </summary>
        private void FireImmediatePropertyChange(string propertyName, object? oldValue, object? newValue)
        {
            if (_currentElement == null || _isLoading) return;
            if (!Equals(oldValue, newValue))
            {
                PropertyChangeCommitted?.Invoke(this, new PropertyChangedUndoEventArgs(
                    _currentElement, propertyName, oldValue, newValue));
            }
        }

        /// <summary>
        /// Set the asset service for font loading
        /// </summary>
        public void SetAssetService(IAssetService assetService)
        {
            _assetService = assetService;
        }

        private void PropBgColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentElement == null) return;
            var oldColor = _currentElement.BackgroundColor;
            var dialog = new ColorPickerDialog(_currentElement.BackgroundColor);
            if (dialog.ShowDialog() == true)
            {
                _currentElement.BackgroundColor = dialog.SelectedColor;
                PropBgColorPreview.Fill = new SolidColorBrush(dialog.SelectedColor);
                NotifyPropertyChanged();
                FireImmediatePropertyChange("BackgroundColor", oldColor, dialog.SelectedColor);
            }
        }

        private void PropImageTintButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentElement == null) return;
            var oldColor = _currentElement.ImageTintColor;
            var dialog = new ColorPickerDialog(_currentElement.ImageTintColor);
            if (dialog.ShowDialog() == true)
            {
                _currentElement.ImageTintColor = dialog.SelectedColor;
                PropImageTintPreview.Fill = new SolidColorBrush(dialog.SelectedColor);
                NotifyPropertyChanged();
                FireImmediatePropertyChange("ImageTintColor", oldColor, dialog.SelectedColor);
            }
        }

        private void PropTextColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentElement == null) return;
            var oldColor = _currentElement.TextColor;
            var dialog = new ColorPickerDialog(_currentElement.TextColor);
            if (dialog.ShowDialog() == true)
            {
                _currentElement.TextColor = dialog.SelectedColor;
                PropTextColorPreview.Fill = new SolidColorBrush(dialog.SelectedColor);
                NotifyPropertyChanged();
                FireImmediatePropertyChange("TextColor", oldColor, dialog.SelectedColor);
            }
        }

        private void PropTextOutlineColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentElement == null) return;
            var oldColor = _currentElement.TextOutlineColor;
            var dialog = new ColorPickerDialog(_currentElement.TextOutlineColor);
            if (dialog.ShowDialog() == true)
            {
                _currentElement.TextOutlineColor = dialog.SelectedColor;
                PropTextOutlineColorPreview.Fill = new SolidColorBrush(dialog.SelectedColor);
                NotifyPropertyChanged();
                FireImmediatePropertyChange("TextOutlineColor", oldColor, dialog.SelectedColor);
            }
        }

        private void PropCaretColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentElement == null) return;
            var oldColor = _currentElement.CaretColor;
            var dialog = new ColorPickerDialog(_currentElement.CaretColor);
            if (dialog.ShowDialog() == true)
            {
                _currentElement.CaretColor = dialog.SelectedColor;
                PropCaretColorPreview.Fill = new SolidColorBrush(dialog.SelectedColor);
                NotifyPropertyChanged();
                FireImmediatePropertyChange("CaretColor", oldColor, dialog.SelectedColor);
            }
        }

        private void PropSelectionColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentElement == null) return;
            var oldColor = _currentElement.SelectionColor;
            var dialog = new ColorPickerDialog(_currentElement.SelectionColor);
            if (dialog.ShowDialog() == true)
            {
                _currentElement.SelectionColor = dialog.SelectedColor;
                PropSelectionColorPreview.Fill = new SolidColorBrush(dialog.SelectedColor);
                NotifyPropertyChanged();
                FireImmediatePropertyChange("SelectionColor", oldColor, dialog.SelectedColor);
            }
        }

        private void PropIMESelectionColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentElement == null) return;
            var oldColor = _currentElement.IMESelectionColor;
            var dialog = new ColorPickerDialog(_currentElement.IMESelectionColor);
            if (dialog.ShowDialog() == true)
            {
                _currentElement.IMESelectionColor = dialog.SelectedColor;
                PropIMESelectionColorPreview.Fill = new SolidColorBrush(dialog.SelectedColor);
                NotifyPropertyChanged();
                FireImmediatePropertyChange("IMESelectionColor", oldColor, dialog.SelectedColor);
            }
        }

        private void PropScrollBarColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentElement == null) return;
            var oldColor = _currentElement.ScrollBarColor;
            var dialog = new ColorPickerDialog(_currentElement.ScrollBarColor);
            if (dialog.ShowDialog() == true)
            {
                _currentElement.ScrollBarColor = dialog.SelectedColor;
                PropScrollBarColorPreview.Fill = new SolidColorBrush(dialog.SelectedColor);
                NotifyPropertyChanged();
                FireImmediatePropertyChange("ScrollBarColor", oldColor, dialog.SelectedColor);
            }
        }

        private void PropOverlayColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentElement == null) return;
            var oldColor = _currentElement.OverlayColor;
            var dialog = new ColorPickerDialog(_currentElement.OverlayColor);
            if (dialog.ShowDialog() == true)
            {
                _currentElement.OverlayColor = dialog.SelectedColor;
                PropOverlayColorPreview.Fill = new SolidColorBrush(dialog.SelectedColor);
                NotifyPropertyChanged();
                FireImmediatePropertyChange("OverlayColor", oldColor, dialog.SelectedColor);
            }
        }

        private void PropBorderColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentElement == null) return;
            var oldColor = _currentElement.BorderColor;
            var dialog = new ColorPickerDialog(_currentElement.BorderColor);
            if (dialog.ShowDialog() == true)
            {
                _currentElement.BorderColor = dialog.SelectedColor;
                PropBorderColorPreview.Fill = new SolidColorBrush(dialog.SelectedColor);
                NotifyPropertyChanged();
                FireImmediatePropertyChange("BorderColor", oldColor, dialog.SelectedColor);
            }
        }

        /// <summary>
        /// Get the image mode (Texture or SpriteSheet) for a given browse target
        /// </summary>
        private string? GetImageModeForTarget(string target)
        {
            if (_currentElement == null) return null;

            return target switch
            {
                // ImageElement
                "ImageSource" => _currentElement.ImageAssetType,

                // Button states
                "ButtonNotPressed" or "ButtonMouseOver" or "ButtonPressed" => _currentElement.ButtonImageMode,

                // EditText states
                "EditTextActive" or "EditTextInactive" or "EditTextMouseOver" => _currentElement.EditTextImageMode,

                // Slider images
                "SliderTrackBg" or "SliderTrackFg" or "SliderThumb" or "SliderMouseOverThumb" or "SliderTick" => _currentElement.SliderImageMode,

                // ToggleButton states
                "ToggleChecked" or "ToggleUnchecked" or "ToggleIndeterminate" => _currentElement.ToggleImageMode,

                // ContentDecorator - doesn't have a mode selector, show all
                "BackgroundImage" => null,

                _ => null
            };
        }

        private void BrowseForAsset(string assetType, string target)
        {
            if (GetProjectAssets == null)
            {
                MessageBox.Show("Please connect to a Stride project first (File → Connect to Project).",
                    "No Project Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var allAssets = GetProjectAssets();
            if (allAssets.Count == 0)
            {
                MessageBox.Show("No assets found in the connected project.",
                    "No Assets", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Filter by asset type
            List<AssetItem> filteredAssets;

            if (assetType == "Image")
            {
                // Get the image mode based on target element type
                var imageMode = GetImageModeForTarget(target);

                // Filter based on selected mode
                if (imageMode == "Texture")
                    filteredAssets = allAssets.Where(a => a.Type == "Texture").ToList();
                else if (imageMode == "SpriteSheet")
                    filteredAssets = allAssets.Where(a => a.Type == "SpriteSheet").ToList();
                else
                    filteredAssets = allAssets.Where(a => a.Type == "Texture" || a.Type == "SpriteSheet").ToList();
            }
            else if (assetType == "Font")
            {
                filteredAssets = allAssets.Where(a => a.Type == "SpriteFont").ToList();
            }
            else
            {
                filteredAssets = new List<AssetItem>();
            }

            if (filteredAssets.Count == 0)
            {
                var imageMode = assetType == "Image" ? GetImageModeForTarget(target) : null;
                var typeDescription = imageMode != null ? $"{imageMode}" : assetType;
                MessageBox.Show($"No {typeDescription} assets found in the project.",
                    "No Assets", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var picker = new AssetPickerDialog(filteredAssets);
            if (picker.ShowDialog() == true && picker.SelectedAsset != null && _currentElement != null)
            {
                // Update the appropriate property based on target
                // IMPORTANT: Set both Name (for display) and AssetReference (for export)
                var assetRef = picker.SelectedAsset.AssetReference;
                var assetName = picker.SelectedAsset.Name;
                var assetTypeName = picker.SelectedAsset.Type;

                switch (target)
                {
                    case "ImageSource":
                        _currentElement.ImageSource = assetName;
                        _currentElement.ImageAssetReference = assetRef;
                        _currentElement.ImageAssetType = assetTypeName;
                        PropImageSource.Text = assetName;
                        UpdateSpriteFrameVisibility(assetName);
                        break;

                    case "ButtonNotPressed":
                        _currentElement.ButtonNotPressedImage = assetName;
                        _currentElement.ButtonNotPressedImageAsset = assetRef;
                        PropButtonNotPressedImage.Text = assetName;
                        break;

                    case "ButtonMouseOver":
                        _currentElement.ButtonMouseOverImage = assetName;
                        _currentElement.ButtonMouseOverImageAsset = assetRef;
                        PropButtonMouseOverImage.Text = assetName;
                        break;

                    case "ButtonPressed":
                        _currentElement.ButtonPressedImage = assetName;
                        _currentElement.ButtonPressedImageAsset = assetRef;
                        PropButtonPressedImage.Text = assetName;
                        break;

                    // EditText states
                    case "EditTextActive":
                        _currentElement.EditTextActiveImage = assetName;
                        _currentElement.EditTextActiveImageAsset = assetRef;
                        PropEditTextActiveImage.Text = assetName;
                        break;

                    case "EditTextInactive":
                        _currentElement.EditTextInactiveImage = assetName;
                        _currentElement.EditTextInactiveImageAsset = assetRef;
                        PropEditTextInactiveImage.Text = assetName;
                        break;

                    case "EditTextMouseOver":
                        _currentElement.EditTextMouseOverImage = assetName;
                        _currentElement.EditTextMouseOverImageAsset = assetRef;
                        PropEditTextMouseOverImage.Text = assetName;
                        break;

                    // Slider images
                    case "SliderTrackBg":
                        _currentElement.SliderTrackBackgroundImage = assetName;
                        _currentElement.SliderTrackBackgroundImageAsset = assetRef;
                        PropSliderTrackBgImage.Text = assetName;
                        break;

                    case "SliderTrackFg":
                        _currentElement.SliderTrackForegroundImage = assetName;
                        _currentElement.SliderTrackForegroundImageAsset = assetRef;
                        PropSliderTrackFgImage.Text = assetName;
                        break;

                    case "SliderThumb":
                        _currentElement.SliderThumbImage = assetName;
                        _currentElement.SliderThumbImageAsset = assetRef;
                        PropSliderThumbImage.Text = assetName;
                        break;

                    case "SliderMouseOverThumb":
                        _currentElement.SliderMouseOverThumbImage = assetName;
                        _currentElement.SliderMouseOverThumbImageAsset = assetRef;
                        PropSliderMouseOverThumbImage.Text = assetName;
                        break;

                    case "SliderTick":
                        _currentElement.SliderTickImage = assetName;
                        _currentElement.SliderTickImageAsset = assetRef;
                        PropSliderTickImage.Text = assetName;
                        break;

                    // ToggleButton states
                    case "ToggleChecked":
                        _currentElement.ToggleCheckedImage = assetName;
                        _currentElement.ToggleCheckedImageAsset = assetRef;
                        PropToggleCheckedImage.Text = assetName;
                        break;

                    case "ToggleUnchecked":
                        _currentElement.ToggleUncheckedImage = assetName;
                        _currentElement.ToggleUncheckedImageAsset = assetRef;
                        PropToggleUncheckedImage.Text = assetName;
                        break;

                    case "ToggleIndeterminate":
                        _currentElement.ToggleIndeterminateImage = assetName;
                        _currentElement.ToggleIndeterminateImageAsset = assetRef;
                        PropToggleIndeterminateImage.Text = assetName;
                        break;

                    // ContentDecorator background image
                    case "BackgroundImage":
                        _currentElement.BackgroundImageSource = assetName;
                        _currentElement.BackgroundImageAsset = assetRef;
                        PropBackgroundImage.Text = assetName;
                        break;

                    // Font
                    case "Font":
                        // Use the Reference property for proper "guid:path" format
                        var fontRef = assetRef as AssetReference;
                        _currentElement.FontAssetReference = fontRef?.Reference;
                        _currentElement.FontFamily = assetName;
                        PropFontFamily.Text = assetName;
                        break;
                }
            }
        }

        public void LoadElement(UIElementViewModel element)
        {
            _currentElement = element;

            if (element == null)
            {
                NoSelectionText.Visibility = Visibility.Visible;
                PropertyCategories.Visibility = Visibility.Collapsed;
                return;
            }

            // Suppress property change events during loading
            _isLoading = true;

            try
            {
                NoSelectionText.Visibility = Visibility.Collapsed;
                PropertyCategories.Visibility = Visibility.Visible;

                // Load common properties
                PropName.Text = element.Name;
                PropIsLocked.IsChecked = element.IsLocked;
                PropAllowOverflow.IsChecked = element.AllowCanvasOverflow;
            PropType.Text = element.ElementType;
            PropId.Text = element.Id;

            // Layout
            PropX.Text = element.X.ToString("F2");
            PropY.Text = element.Y.ToString("F2");
            PropWidth.Text = element.Width.ToString("F2");
            PropHeight.Text = element.Height.ToString("F2");

            SetComboBoxValue(PropHAlign, element.HorizontalAlignment);
            SetComboBoxValue(PropVAlign, element.VerticalAlignment);

            // Appearance
            PropBgColorPreview.Fill = new SolidColorBrush(element.BackgroundColor);
            PropOpacity.Value = element.Opacity;
            PropOpacityText.Text = $"{(int)(element.Opacity * 100)}%";
            PropZIndex.Text = element.ZIndex.ToString();
            PropDrawLayer.Text = element.DrawLayerNumber.ToString();
            PropClipToBounds.IsChecked = element.ClipToBounds;

            // Behavior
            SetComboBoxValue(PropVisibility, element.Visibility);
            PropIsEnabled.IsChecked = element.IsEnabled;
            PropCanBeHitByUser.IsChecked = element.CanBeHitByUser;

            // Hide all element-specific panels
            ImageProperties.Visibility = Visibility.Collapsed;
            TextProperties.Visibility = Visibility.Collapsed;
            ButtonProperties.Visibility = Visibility.Collapsed;
            EditTextProperties.Visibility = Visibility.Collapsed;
            SliderProperties.Visibility = Visibility.Collapsed;
            ToggleButtonProperties.Visibility = Visibility.Collapsed;
            StackPanelProperties.Visibility = Visibility.Collapsed;
            GridProperties.Visibility = Visibility.Collapsed;
            GridCellProperties.Visibility = Visibility.Collapsed;
            ScrollViewerProperties.Visibility = Visibility.Collapsed;
            ModalProperties.Visibility = Visibility.Collapsed;
            UniformGridProperties.Visibility = Visibility.Collapsed;
            ContentDecoratorProperties.Visibility = Visibility.Collapsed;

            // Show element-specific properties
            switch (element.ElementType)
            {
                case "ImageElement":
                    LoadImageProperties(element);
                    break;
                case "TextBlock":
                    LoadTextProperties(element);
                    break;
                case "Button":
                    LoadButtonProperties(element);
                    break;
                case "EditText":
                    LoadEditTextProperties(element);
                    break;
                case "Slider":
                    LoadSliderProperties(element);
                    break;
                case "ToggleButton":
                    LoadToggleButtonProperties(element);
                    break;
                case "StackPanel":
                    LoadStackPanelProperties(element);
                    break;
                case "Grid":
                    LoadGridProperties(element);
                    break;
                case "ScrollViewer":
                    LoadScrollViewerProperties(element);
                    break;
                case "ModalElement":
                    LoadModalProperties(element);
                    break;
                case "UniformGrid":
                    LoadUniformGridProperties(element);
                    break;
                case "ContentDecorator":
                case "Border":
                    LoadContentDecoratorProperties(element);
                    break;
                // Canvas only needs common properties
            }

            // Show GridCell properties if element's parent is a Grid
            if (element.Parent != null && element.Parent.ElementType == "Grid")
            {
                LoadGridCellProperties(element);
            }
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void LoadImageProperties(UIElementViewModel element)
        {
            ImageProperties.Visibility = Visibility.Visible;
            SetComboBoxValue(PropImageSourceMode, element.ImageAssetType);
            PropImageSource.Text = element.ImageSource;
            PropSpriteFrame.Text = element.SpriteFrame.ToString();
            SetComboBoxValue(PropStretchType, element.StretchType);
            SetComboBoxValue(PropStretchDirection, element.StretchDirection);
            PropImageTintPreview.Fill = new SolidColorBrush(element.ImageTintColor);

            UpdateSpriteFrameVisibility(element.ImageSource);
        }

        private void UpdateSpriteFrameVisibility(string imageName)
        {
            // Default to visible
            SpriteFrameContainer.Visibility = Visibility.Visible;

            if (GetProjectAssets != null && !string.IsNullOrEmpty(imageName))
            {
                var assets = GetProjectAssets();
                var asset = assets.FirstOrDefault(a => a.Name == imageName);
                if (asset != null && asset.Type == "Texture")
                {
                    // Hide frame property for single textures
                    SpriteFrameContainer.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void LoadTextProperties(UIElementViewModel element)
        {
            TextProperties.Visibility = Visibility.Visible;
            PropText.Text = element.Text;
            PropFontSize.Text = ((int)element.FontSize).ToString();
            PropTextColorPreview.Fill = new SolidColorBrush(element.TextColor);
            SetComboBoxValue(PropTextAlign, element.TextAlignment);
            PropWrapText.IsChecked = element.WrapText;
            PropDoNotSnapText.IsChecked = element.DoNotSnapText;

            PropTextOutlineColorPreview.Fill = new SolidColorBrush(element.TextOutlineColor);
            PropTextOutlineThickness.Text = element.TextOutlineThickness.ToString("F2");

            // Set font family display name
            PropFontFamily.Text = element.FontFamily;
        }

        private void LoadButtonProperties(UIElementViewModel element)
        {
            ButtonProperties.Visibility = Visibility.Visible;
            PropButtonText.Text = element.ButtonText;
            SetComboBoxValue(PropClickMode, element.ClickMode);
            SetComboBoxValue(PropButtonImageMode, element.ButtonImageMode);

            // 3-State Images
            PropButtonNotPressedImage.Text = element.ButtonNotPressedImage;
            PropButtonNotPressedFrame.Text = element.ButtonNotPressedFrame.ToString();
            PropButtonMouseOverImage.Text = element.ButtonMouseOverImage;
            PropButtonMouseOverFrame.Text = element.ButtonMouseOverFrame.ToString();
            PropButtonPressedImage.Text = element.ButtonPressedImage;
            PropButtonPressedFrame.Text = element.ButtonPressedFrame.ToString();
        }

        private void LoadEditTextProperties(UIElementViewModel element)
        {
            EditTextProperties.Visibility = Visibility.Visible;
            PropEditTextPlaceholder.Text = element.Text; // EditText uses Text property as placeholder
            PropMaxLength.Text = element.MaxLength.ToString();
            PropIsReadOnly.IsChecked = element.IsReadOnly;
            SetComboBoxValue(PropInputType, element.InputType);
            PropMinLines.Text = element.MinLines.ToString();
            PropMaxLines.Text = element.MaxLines.ToString();
            PropCaretWidth.Text = element.CaretWidth.ToString("F2");
            PropCaretFrequency.Text = element.CaretFrequency.ToString("F2");
            PropCaretColorPreview.Fill = new SolidColorBrush(element.CaretColor);
            PropSelectionColorPreview.Fill = new SolidColorBrush(element.SelectionColor);
            PropIMESelectionColorPreview.Fill = new SolidColorBrush(element.IMESelectionColor);
            SetComboBoxValue(PropEditTextImageMode, element.EditTextImageMode);

            // 3-State Images
            PropEditTextActiveImage.Text = element.EditTextActiveImage;
            PropEditTextActiveFrame.Text = element.EditTextActiveFrame.ToString();
            PropEditTextInactiveImage.Text = element.EditTextInactiveImage;
            PropEditTextInactiveFrame.Text = element.EditTextInactiveFrame.ToString();
            PropEditTextMouseOverImage.Text = element.EditTextMouseOverImage;
            PropEditTextMouseOverFrame.Text = element.EditTextMouseOverFrame.ToString();
        }

        private void LoadSliderProperties(UIElementViewModel element)
        {
            SliderProperties.Visibility = Visibility.Visible;
            PropSliderMinimum.Text = element.SliderMinimum.ToString("F2");
            PropSliderMaximum.Text = element.SliderMaximum.ToString("F2");
            PropSliderValue.Text = element.SliderValue.ToString("F2");
            PropSliderStep.Text = element.SliderStep.ToString("F2");
            PropSliderTickFrequency.Text = element.SliderTickFrequency.ToString("F2");
            PropSliderTickOffset.Text = element.SliderTickOffset.ToString("F2");
            SetComboBoxValue(PropSliderOrientation, element.SliderOrientation);
            PropAreTicksDisplayed.IsChecked = element.AreTicksDisplayed;
            PropShouldSnapToTicks.IsChecked = element.ShouldSnapToTicks;
            PropIsDirectionReversed.IsChecked = element.IsDirectionReversed;
            PropTrackStartingOffsetLeft.Text = element.TrackStartingOffsetLeft.ToString("F2");
            PropTrackStartingOffsetTop.Text = element.TrackStartingOffsetTop.ToString("F2");
            PropTrackStartingOffsetRight.Text = element.TrackStartingOffsetRight.ToString("F2");
            PropTrackStartingOffsetBottom.Text = element.TrackStartingOffsetBottom.ToString("F2");
            SetComboBoxValue(PropSliderImageMode, element.SliderImageMode);

            // Slider Images
            PropSliderTrackBgImage.Text = element.SliderTrackBackgroundImage;
            PropSliderTrackBgFrame.Text = element.SliderTrackBackgroundFrame.ToString();
            PropSliderTrackFgImage.Text = element.SliderTrackForegroundImage;
            PropSliderTrackFgFrame.Text = element.SliderTrackForegroundFrame.ToString();
            PropSliderThumbImage.Text = element.SliderThumbImage;
            PropSliderThumbFrame.Text = element.SliderThumbFrame.ToString();
            PropSliderMouseOverThumbImage.Text = element.SliderMouseOverThumbImage;
            PropSliderMouseOverThumbFrame.Text = element.SliderMouseOverThumbFrame.ToString();
            PropSliderTickImage.Text = element.SliderTickImage;
            PropSliderTickFrame.Text = element.SliderTickFrame.ToString();
        }

        private void LoadToggleButtonProperties(UIElementViewModel element)
        {
            ToggleButtonProperties.Visibility = Visibility.Visible;
            PropToggleButtonText.Text = element.ButtonText; // ToggleButton uses ButtonText property
            SetComboBoxValue(PropToggleState, element.ToggleState);
            SetComboBoxValue(PropToggleClickMode, element.ToggleClickMode);
            PropIsThreeState.IsChecked = element.IsThreeState;
            SetComboBoxValue(PropToggleImageMode, element.ToggleImageMode);

            // 3-State Images
            PropToggleCheckedImage.Text = element.ToggleCheckedImage;
            PropToggleCheckedFrame.Text = element.ToggleCheckedFrame.ToString();
            PropToggleUncheckedImage.Text = element.ToggleUncheckedImage;
            PropToggleUncheckedFrame.Text = element.ToggleUncheckedFrame.ToString();
            PropToggleIndeterminateImage.Text = element.ToggleIndeterminateImage;
            PropToggleIndeterminateFrame.Text = element.ToggleIndeterminateFrame.ToString();
        }

        private void LoadStackPanelProperties(UIElementViewModel element)
        {
            StackPanelProperties.Visibility = Visibility.Visible;
            SetComboBoxValue(PropStackPanelOrientation, element.StackPanelOrientation);
            PropItemVirtualizationEnabled.IsChecked = element.ItemVirtualizationEnabled;
        }

        private void LoadGridProperties(UIElementViewModel element)
        {
            // Hidden: forcing Canvas workflow - values still load for Stride compatibility
            // GridProperties.Visibility = Visibility.Visible;
            PropRowDefinitions.Text = element.RowDefinitions;
            PropColumnDefinitions.Text = element.ColumnDefinitions;
            PropRowSpacing.Text = element.RowSpacing.ToString();
            PropColumnSpacing.Text = element.ColumnSpacing.ToString();
        }

        private void LoadGridCellProperties(UIElementViewModel element)
        {
            // Hidden: forcing Canvas workflow - values still load for Stride compatibility
            // GridCellProperties.Visibility = Visibility.Visible;
            PropGridRow.Text = element.GridRow.ToString();
            PropGridColumn.Text = element.GridColumn.ToString();
            PropGridRowSpan.Text = element.GridRowSpan.ToString();
            PropGridColumnSpan.Text = element.GridColumnSpan.ToString();
        }

        private void LoadScrollViewerProperties(UIElementViewModel element)
        {
            ScrollViewerProperties.Visibility = Visibility.Visible;
            SetComboBoxValue(PropScrollMode, element.ScrollMode);
            PropScrollBarThickness.Text = element.ScrollBarThickness.ToString("F2");
            PropScrollingSpeed.Text = element.ScrollingSpeed.ToString("F2");
            PropDeceleration.Text = element.Deceleration.ToString("F2");
            PropScrollStartThreshold.Text = element.ScrollStartThreshold.ToString("F2");
            PropTouchScrollingEnabled.IsChecked = element.TouchScrollingEnabled;
            PropSnapToAnchors.IsChecked = element.SnapToAnchors;
            PropScrollBarColorPreview.Fill = new SolidColorBrush(element.ScrollBarColor);
        }

        private void LoadModalProperties(UIElementViewModel element)
        {
            ModalProperties.Visibility = Visibility.Visible;
            PropIsModal.IsChecked = element.IsModal;
            PropOverlayColorPreview.Fill = new SolidColorBrush(element.OverlayColor);
        }

        private void LoadUniformGridProperties(UIElementViewModel element)
        {
            // Hidden: forcing Canvas workflow - values still load for Stride compatibility
            // UniformGridProperties.Visibility = Visibility.Visible;
            PropUniformGridRows.Text = element.UniformGridRows.ToString();
            PropUniformGridColumns.Text = element.UniformGridColumns.ToString();
        }

        private void LoadContentDecoratorProperties(UIElementViewModel element)
        {
            ContentDecoratorProperties.Visibility = Visibility.Visible;
            PropBorderColorPreview.Fill = new SolidColorBrush(element.BorderColor);
            PropBorderThicknessLeft.Text = element.BorderThicknessLeft.ToString("F2");
            PropBorderThicknessTop.Text = element.BorderThicknessTop.ToString("F2");
            PropBorderThicknessRight.Text = element.BorderThicknessRight.ToString("F2");
            PropBorderThicknessBottom.Text = element.BorderThicknessBottom.ToString("F2");
            SetComboBoxValue(PropContentDecoratorImageMode, element.ContentDecoratorImageMode);
            PropBackgroundImage.Text = element.BackgroundImageSource;
            PropBackgroundImageFrame.Text = element.BackgroundImageFrame.ToString();
        }

        private void SetComboBoxValue(ComboBox comboBox, string value)
        {
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (((ComboBoxItem)comboBox.Items[i]).Content.ToString() == value)
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }
        }

        private string GetComboBoxValue(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
                return item.Content.ToString() ?? "";
            return "";
        }
    }
}

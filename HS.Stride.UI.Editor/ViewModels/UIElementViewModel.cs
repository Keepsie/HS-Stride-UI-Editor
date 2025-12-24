// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace HS.Stride.UI.Editor.ViewModels
{
    /// <summary>
    /// ViewModel representing a UI element in the visual tree
    /// </summary>
    public class UIElementViewModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _elementType = string.Empty;
        private bool _isSelected;
        private bool _isLocked = false; // Prevents selection on canvas, can still select via hierarchy
        private bool _allowCanvasOverflow = false; // Allow element to be positioned outside canvas bounds
        private bool _isSystemElement = false; // Hidden root Grid container
        private double _x;
        private double _y;
        private double _width = 100;
        private double _height = 100;

        // Common Properties (All Elements)
        private Color _backgroundColor = Colors.Transparent;
        private double _opacity = 1.0;
        private int _drawLayerNumber = 0;
        private int _zIndex = 0; // Panel.ZIndex - controls sibling draw order within a panel
        private string _horizontalAlignment = "Stretch";
        private string _verticalAlignment = "Stretch";
        private double _marginLeft = 0;
        private double _marginTop = 0;
        private double _marginRight = 0;
        private double _marginBottom = 0;
        private bool _clipToBounds = false;
        private bool _isEnabled = true;
        private bool _canBeHitByUser = false;
        private string _visibility = "Visible"; // Visible, Hidden, Collapsed

        // ImageElement Properties
        private string _imageSource = string.Empty;
        private object? _imageAssetReference = null; // Toolkit AssetReference for save/export
        private string _imageAssetType = "Texture"; // "Texture" or "SpriteSheet"
        private int _spriteFrame = 0;
        private Color _imageTintColor = Colors.White;
        private string _stretchType = "FillOnStretch"; // FillOnStretch, Fill, Uniform, UniformToFill
        private System.Windows.Media.ImageSource? _thumbnailImage = null; // Cached thumbnail for hierarchy display
        private string _stretchDirection = "Both"; // Both, UpOnly, DownOnly

        // TextBlock Properties
        private string _text = string.Empty;
        private string _fontFamily = "Arial"; // Default font - matches Stride's default system font
        private string? _fontAssetReference = null; // Asset reference string (guid:path format)
        private double _fontSize = 16;
        private Color _textColor = Color.FromRgb(224, 224, 224); // Light grey (#E0E0E0) for visibility on dark canvas
        private string _textAlignment = "Left";
        private bool _wrapText = false;
        private bool _doNotSnapText = false; // Disable text snapping to pixel grid

        // Button/ToggleButton Properties
        private string _buttonText = ""; // Used by ToggleButton for label
        private string _clickMode = "Release"; // Release, Press, Hover
        private string _buttonImageMode = "SpriteSheet"; // "Texture" or "SpriteSheet"

        // Button 3-State Images (NotPressed, Pressed, MouseOver)
        private string _buttonNotPressedImage = string.Empty;
        private int _buttonNotPressedFrame = 0;
        private object? _buttonNotPressedImageAsset = null;
        private string _buttonPressedImage = string.Empty;
        private int _buttonPressedFrame = 0;
        private object? _buttonPressedImageAsset = null;
        private string _buttonMouseOverImage = string.Empty;
        private int _buttonMouseOverFrame = 0;
        private object? _buttonMouseOverImageAsset = null;

        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string ElementType
        {
            get => _elementType;
            set { _elementType = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public bool IsSystemElement
        {
            get => _isSystemElement;
            set { _isSystemElement = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// When locked, element cannot be selected by clicking on canvas.
        /// Can still be selected via hierarchy tree.
        /// </summary>
        public bool IsLocked
        {
            get => _isLocked;
            set { _isLocked = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// When true, element can be positioned outside the canvas bounds.
        /// Useful for overlays, HUD elements that bleed to screen edges, etc.
        /// </summary>
        public bool AllowCanvasOverflow
        {
            get => _allowCanvasOverflow;
            set { _allowCanvasOverflow = value; OnPropertyChanged(); }
        }

        // Layout Properties
        public double X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(); }
        }

        public double Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(); }
        }

        public double Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(); }
        }

        public double Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(); }
        }

        // Common Appearance Properties
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set { _backgroundColor = value; OnPropertyChanged(); }
        }

        public double Opacity
        {
            get => _opacity;
            set { _opacity = Math.Clamp(value, 0.0, 1.0); OnPropertyChanged(); }
        }

        public int DrawLayerNumber
        {
            get => _drawLayerNumber;
            set { _drawLayerNumber = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Panel.ZIndex - controls sibling draw order within a panel.
        /// Higher values are drawn on top of lower values.
        /// </summary>
        public int ZIndex
        {
            get => _zIndex;
            set { _zIndex = value; OnPropertyChanged(); }
        }

        public string HorizontalAlignment
        {
            get => _horizontalAlignment;
            set { _horizontalAlignment = value; OnPropertyChanged(); RecalculatePositionFromAlignment(); }
        }

        public string VerticalAlignment
        {
            get => _verticalAlignment;
            set { _verticalAlignment = value; OnPropertyChanged(); RecalculatePositionFromAlignment(); }
        }

        // Margin properties (Stride uses margin + alignment to determine position)
        public double MarginLeft
        {
            get => _marginLeft;
            set { _marginLeft = value; OnPropertyChanged(); RecalculatePositionFromAlignment(); }
        }

        public double MarginTop
        {
            get => _marginTop;
            set { _marginTop = value; OnPropertyChanged(); RecalculatePositionFromAlignment(); }
        }

        public double MarginRight
        {
            get => _marginRight;
            set { _marginRight = value; OnPropertyChanged(); RecalculatePositionFromAlignment(); }
        }

        public double MarginBottom
        {
            get => _marginBottom;
            set { _marginBottom = value; OnPropertyChanged(); RecalculatePositionFromAlignment(); }
        }

        /// <summary>
        /// Recalculates X/Y position based on alignment, margin, and parent dimensions.
        /// This matches Stride's layout behavior where alignment + margin determines position.
        /// </summary>
        public void RecalculatePositionFromAlignment()
        {
            if (Parent == null) return;

            var parentWidth = Parent.Width;
            var parentHeight = Parent.Height;
            var parentType = Parent.ElementType;

            // Canvas children: margin IS the absolute position, alignment is ignored
            if (parentType == "Canvas")
            {
                _x = _marginLeft;
                _y = _marginTop;
                OnPropertyChanged(nameof(X));
                OnPropertyChanged(nameof(Y));
                return;
            }

            // For Grid and other containers, calculate position based on alignment
            double x, y;

            // Horizontal position
            switch (_horizontalAlignment)
            {
                case "Left":
                    x = _marginLeft;
                    break;
                case "Right":
                    x = parentWidth - _marginRight - _width;
                    break;
                case "Center":
                    x = _marginLeft + (parentWidth - _width - _marginLeft - _marginRight) / 2;
                    break;
                case "Stretch":
                default:
                    x = _marginLeft;
                    break;
            }

            // Vertical position
            switch (_verticalAlignment)
            {
                case "Top":
                    y = _marginTop;
                    break;
                case "Bottom":
                    y = parentHeight - _marginBottom - _height;
                    break;
                case "Center":
                    y = _marginTop + (parentHeight - _height - _marginTop - _marginBottom) / 2;
                    break;
                case "Stretch":
                default:
                    y = _marginTop;
                    break;
            }

            _x = x;
            _y = y;
            OnPropertyChanged(nameof(X));
            OnPropertyChanged(nameof(Y));
        }

        /// <summary>
        /// Updates margin values based on current X/Y position and alignment.
        /// This is the reverse of RecalculatePositionFromAlignment - converts visual position back to margin.
        /// Call this before saving to ensure margins match the visual position.
        ///
        /// Stride margin behavior:
        /// - Left alignment: marginLeft = distance from left edge (marginLeft = X)
        /// - Right alignment: marginRight = distance from right edge
        /// - Top alignment: marginTop = distance from top edge (marginTop = Y)
        /// - Bottom alignment: marginBottom = distance from bottom edge
        /// - Center: margins define offset from center
        /// - Stretch: margins define insets from both edges
        /// </summary>
        public void UpdateMarginFromPosition()
        {
            var parentWidth = Parent?.Width ?? 1280;
            var parentHeight = Parent?.Height ?? 720;
            var parentType = Parent?.ElementType ?? "Grid";

            // Canvas: margin IS the absolute position
            if (parentType == "Canvas")
            {
                _marginLeft = _x;
                _marginTop = _y;
                _marginRight = 0;
                _marginBottom = 0;
                return;
            }

            // For Grid and other containers, only set the margin relevant to the alignment
            // Clear the opposite margin to avoid conflicts
            switch (_horizontalAlignment)
            {
                case "Left":
                    _marginLeft = _x;
                    _marginRight = 0;
                    break;
                case "Right":
                    _marginRight = parentWidth - _x - _width;
                    _marginLeft = 0;
                    break;
                case "Center":
                    // Center uses equal margins from both sides, keep as-is or calculate from offset
                    // For now, just use left margin as offset hint
                    _marginLeft = _x - (parentWidth - _width) / 2;
                    _marginRight = _marginLeft;
                    break;
                case "Stretch":
                    // Stretch: margins define insets from edges
                    _marginLeft = _x;
                    _marginRight = parentWidth - _x - _width;
                    break;
            }

            switch (_verticalAlignment)
            {
                case "Top":
                    _marginTop = _y;
                    _marginBottom = 0;
                    break;
                case "Bottom":
                    _marginBottom = parentHeight - _y - _height;
                    _marginTop = 0;
                    break;
                case "Center":
                    _marginTop = _y - (parentHeight - _height) / 2;
                    _marginBottom = _marginTop;
                    break;
                case "Stretch":
                    _marginTop = _y;
                    _marginBottom = parentHeight - _y - _height;
                    break;
            }
        }

        /// <summary>
        /// Sets margin values without triggering position recalculation.
        /// Used during load to set both margin and position correctly.
        /// </summary>
        public void SetMarginWithoutRecalculation(double left, double top, double right, double bottom)
        {
            _marginLeft = left;
            _marginTop = top;
            _marginRight = right;
            _marginBottom = bottom;
        }

        /// <summary>
        /// Sets X/Y position without updating margin values.
        /// Used during load to set position correctly without overwriting loaded margins.
        /// </summary>
        public void SetPositionWithoutMarginUpdate(double x, double y)
        {
            _x = x;
            _y = y;
            OnPropertyChanged(nameof(X));
            OnPropertyChanged(nameof(Y));
        }

        public bool ClipToBounds
        {
            get => _clipToBounds;
            set { _clipToBounds = value; OnPropertyChanged(); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        public bool CanBeHitByUser
        {
            get => _canBeHitByUser;
            set { _canBeHitByUser = value; OnPropertyChanged(); }
        }

        public string Visibility
        {
            get => _visibility;
            set { _visibility = value; OnPropertyChanged(); }
        }

        // ImageElement Properties
        public string ImageSource
        {
            get => _imageSource;
            set { _imageSource = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Toolkit AssetReference for the image source (used during save/export)
        /// </summary>
        public object? ImageAssetReference
        {
            get => _imageAssetReference;
            set { _imageAssetReference = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Type of image asset: "Texture" or "SpriteSheet"
        /// </summary>
        public string ImageAssetType
        {
            get => _imageAssetType;
            set { _imageAssetType = value; OnPropertyChanged(); }
        }

        public int SpriteFrame
        {
            get => _spriteFrame;
            set { _spriteFrame = value; OnPropertyChanged(); }
        }

        public Color ImageTintColor
        {
            get => _imageTintColor;
            set { _imageTintColor = value; OnPropertyChanged(); }
        }

        public string StretchType
        {
            get => _stretchType;
            set { _stretchType = value; OnPropertyChanged(); }
        }

        public string StretchDirection
        {
            get => _stretchDirection;
            set { _stretchDirection = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Cached thumbnail image for hierarchy display (set by UIElementVisual when image loads)
        /// </summary>
        public System.Windows.Media.ImageSource? ThumbnailImage
        {
            get => _thumbnailImage;
            set { _thumbnailImage = value; OnPropertyChanged(); }
        }

        // TextBlock Properties
        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        public string FontFamily
        {
            get => _fontFamily;
            set { _fontFamily = value; OnPropertyChanged(); }
        }

        public double FontSize
        {
            get => _fontSize;
            set { _fontSize = value; OnPropertyChanged(); }
        }

        public Color TextColor
        {
            get => _textColor;
            set { _textColor = value; OnPropertyChanged(); }
        }

        public string TextAlignment
        {
            get => _textAlignment;
            set { _textAlignment = value; OnPropertyChanged(); }
        }

        public bool WrapText
        {
            get => _wrapText;
            set { _wrapText = value; OnPropertyChanged(); }
        }

        public bool DoNotSnapText
        {
            get => _doNotSnapText;
            set { _doNotSnapText = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Asset reference string for the font (guid:path format, used during save/export)
        /// </summary>
        public string? FontAssetReference
        {
            get => _fontAssetReference;
            set { _fontAssetReference = value; OnPropertyChanged(); }
        }

        // Button/ToggleButton Properties
        public string ButtonText  // Used by ToggleButton for label
        {
            get => _buttonText;
            set { _buttonText = value; OnPropertyChanged(); }
        }

        public string ClickMode
        {
            get => _clickMode;
            set { _clickMode = value; OnPropertyChanged(); }
        }

        public string ButtonImageMode
        {
            get => _buttonImageMode;
            set { _buttonImageMode = value; OnPropertyChanged(); }
        }

        // Button 3-State Images
        public string ButtonNotPressedImage
        {
            get => _buttonNotPressedImage;
            set { _buttonNotPressedImage = value; OnPropertyChanged(); }
        }

        public int ButtonNotPressedFrame
        {
            get => _buttonNotPressedFrame;
            set { _buttonNotPressedFrame = value; OnPropertyChanged(); }
        }

        public string ButtonPressedImage
        {
            get => _buttonPressedImage;
            set { _buttonPressedImage = value; OnPropertyChanged(); }
        }

        public int ButtonPressedFrame
        {
            get => _buttonPressedFrame;
            set { _buttonPressedFrame = value; OnPropertyChanged(); }
        }

        public string ButtonMouseOverImage
        {
            get => _buttonMouseOverImage;
            set { _buttonMouseOverImage = value; OnPropertyChanged(); }
        }

        public int ButtonMouseOverFrame
        {
            get => _buttonMouseOverFrame;
            set { _buttonMouseOverFrame = value; OnPropertyChanged(); }
        }

        // Button AssetReference properties (for export using toolkit helpers)
        public object? ButtonNotPressedImageAsset
        {
            get => _buttonNotPressedImageAsset;
            set { _buttonNotPressedImageAsset = value; OnPropertyChanged(); }
        }

        public object? ButtonPressedImageAsset
        {
            get => _buttonPressedImageAsset;
            set { _buttonPressedImageAsset = value; OnPropertyChanged(); }
        }

        public object? ButtonMouseOverImageAsset
        {
            get => _buttonMouseOverImageAsset;
            set { _buttonMouseOverImageAsset = value; OnPropertyChanged(); }
        }

        // TextBlock Additional Properties
        private Color _textOutlineColor = Colors.Black;
        private double _textOutlineThickness = 0.0;

        public Color TextOutlineColor
        {
            get => _textOutlineColor;
            set { _textOutlineColor = value; OnPropertyChanged(); }
        }

        public double TextOutlineThickness
        {
            get => _textOutlineThickness;
            set { _textOutlineThickness = value; OnPropertyChanged(); }
        }

        // StackPanel Properties
        private string _stackPanelOrientation = "Vertical";
        private bool _itemVirtualizationEnabled = false;

        public string StackPanelOrientation
        {
            get => _stackPanelOrientation;
            set { _stackPanelOrientation = value; OnPropertyChanged(); }
        }

        public bool ItemVirtualizationEnabled
        {
            get => _itemVirtualizationEnabled;
            set { _itemVirtualizationEnabled = value; OnPropertyChanged(); }
        }

        // Grid Properties (for Grid containers)
        private string _rowDefinitions = "1*"; // Comma-separated: "1*,2*,100" = 1 star, 2 stars, 100px
        private string _columnDefinitions = "1*"; // Comma-separated: "1*,2*,100"
        private double _rowSpacing = 0;
        private double _columnSpacing = 0;

        public string RowDefinitions
        {
            get => _rowDefinitions;
            set { _rowDefinitions = value; OnPropertyChanged(); }
        }

        public string ColumnDefinitions
        {
            get => _columnDefinitions;
            set { _columnDefinitions = value; OnPropertyChanged(); }
        }

        public double RowSpacing
        {
            get => _rowSpacing;
            set { _rowSpacing = value; OnPropertyChanged(); }
        }

        public double ColumnSpacing
        {
            get => _columnSpacing;
            set { _columnSpacing = value; OnPropertyChanged(); }
        }

        // Grid Cell Properties (for elements inside a Grid)
        private int _gridRow = 0;
        private int _gridColumn = 0;
        private int _gridRowSpan = 1;
        private int _gridColumnSpan = 1;

        public int GridRow
        {
            get => _gridRow;
            set { _gridRow = Math.Max(0, value); OnPropertyChanged(); }
        }

        public int GridColumn
        {
            get => _gridColumn;
            set { _gridColumn = Math.Max(0, value); OnPropertyChanged(); }
        }

        public int GridRowSpan
        {
            get => _gridRowSpan;
            set { _gridRowSpan = Math.Max(1, value); OnPropertyChanged(); }
        }

        public int GridColumnSpan
        {
            get => _gridColumnSpan;
            set { _gridColumnSpan = Math.Max(1, value); OnPropertyChanged(); }
        }

        // ScrollViewer Properties
        private Color _scrollBarColor = Color.FromRgb(100, 100, 100);
        private string _scrollMode = "Vertical"; // None, Vertical, Horizontal, VerticalHorizontal
        private double _scrollBarThickness = 6.0;  // Stride default is 6.0
        private bool _touchScrollingEnabled = true;
        private double _deceleration = 1500.0;  // Stride default is 1500.0 (not 0.9)
        private bool _snapToAnchors = false;    // Stride default is false
        private double _scrollingSpeed = 800.0;
        private double _scrollStartThreshold = 10.0;  // Stride default is 10.0

        public Color ScrollBarColor
        {
            get => _scrollBarColor;
            set { _scrollBarColor = value; OnPropertyChanged(); }
        }

        public string ScrollMode
        {
            get => _scrollMode;
            set { _scrollMode = value; OnPropertyChanged(); }
        }

        public double ScrollBarThickness
        {
            get => _scrollBarThickness;
            set { _scrollBarThickness = Math.Max(1, value); OnPropertyChanged(); }
        }

        public bool TouchScrollingEnabled
        {
            get => _touchScrollingEnabled;
            set { _touchScrollingEnabled = value; OnPropertyChanged(); }
        }

        public double Deceleration
        {
            get => _deceleration;
            set { _deceleration = Math.Max(0, value); OnPropertyChanged(); }
        }

        public bool SnapToAnchors
        {
            get => _snapToAnchors;
            set { _snapToAnchors = value; OnPropertyChanged(); }
        }

        public double ScrollingSpeed
        {
            get => _scrollingSpeed;
            set { _scrollingSpeed = Math.Max(0, value); OnPropertyChanged(); }
        }

        public double ScrollStartThreshold
        {
            get => _scrollStartThreshold;
            set { _scrollStartThreshold = Math.Max(0, value); OnPropertyChanged(); }
        }

        // EditText Properties
        private string _editTextImageMode = "SpriteSheet"; // "Texture" or "SpriteSheet"
        private string _editTextActiveImage = string.Empty;
        private int _editTextActiveFrame = 0;
        private object? _editTextActiveImageAsset = null;
        private string _editTextInactiveImage = string.Empty;
        private int _editTextInactiveFrame = 0;
        private object? _editTextInactiveImageAsset = null;
        private string _editTextMouseOverImage = string.Empty;
        private int _editTextMouseOverFrame = 0;
        private object? _editTextMouseOverImageAsset = null;
        private Color _caretColor = Color.FromRgb(240, 240, 240);
        private double _caretWidth = 1.0;  // Stride default is 1.0f
        private Color _selectionColor = Color.FromRgb(51, 153, 255);
        private Color _imeSelectionColor = Color.FromRgb(240, 255, 240);  // Stride default
        private int _maxLength = 0; // 0 = unlimited (int.MaxValue in Stride)
        private bool _isReadOnly = false;
        private int _minLines = 1;
        private int _maxLines = int.MaxValue;
        private string _inputType = "None"; // None, Password, etc.
        private double _caretFrequency = 1.0; // Caret blink frequency in Hz

        public string EditTextImageMode
        {
            get => _editTextImageMode;
            set { _editTextImageMode = value; OnPropertyChanged(); }
        }

        public string EditTextActiveImage
        {
            get => _editTextActiveImage;
            set { _editTextActiveImage = value; OnPropertyChanged(); }
        }

        public int EditTextActiveFrame
        {
            get => _editTextActiveFrame;
            set { _editTextActiveFrame = value; OnPropertyChanged(); }
        }

        public string EditTextInactiveImage
        {
            get => _editTextInactiveImage;
            set { _editTextInactiveImage = value; OnPropertyChanged(); }
        }

        public int EditTextInactiveFrame
        {
            get => _editTextInactiveFrame;
            set { _editTextInactiveFrame = value; OnPropertyChanged(); }
        }

        public string EditTextMouseOverImage
        {
            get => _editTextMouseOverImage;
            set { _editTextMouseOverImage = value; OnPropertyChanged(); }
        }

        public int EditTextMouseOverFrame
        {
            get => _editTextMouseOverFrame;
            set { _editTextMouseOverFrame = value; OnPropertyChanged(); }
        }

        public Color CaretColor
        {
            get => _caretColor;
            set { _caretColor = value; OnPropertyChanged(); }
        }

        public double CaretWidth
        {
            get => _caretWidth;
            set { _caretWidth = Math.Max(0, value); OnPropertyChanged(); }
        }

        public Color SelectionColor
        {
            get => _selectionColor;
            set { _selectionColor = value; OnPropertyChanged(); }
        }

        public Color IMESelectionColor
        {
            get => _imeSelectionColor;
            set { _imeSelectionColor = value; OnPropertyChanged(); }
        }

        public int MaxLength
        {
            get => _maxLength;
            set { _maxLength = value; OnPropertyChanged(); }
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set { _isReadOnly = value; OnPropertyChanged(); }
        }

        public int MinLines
        {
            get => _minLines;
            set { _minLines = Math.Max(1, value); OnPropertyChanged(); }
        }

        public int MaxLines
        {
            get => _maxLines;
            set { _maxLines = Math.Max(1, value); OnPropertyChanged(); }
        }

        public string InputType
        {
            get => _inputType;
            set { _inputType = value; OnPropertyChanged(); }
        }

        public double CaretFrequency
        {
            get => _caretFrequency;
            set { _caretFrequency = Math.Max(0.1, value); OnPropertyChanged(); }
        }

        // EditText AssetReference properties (for export using toolkit helpers)
        public object? EditTextActiveImageAsset
        {
            get => _editTextActiveImageAsset;
            set { _editTextActiveImageAsset = value; OnPropertyChanged(); }
        }

        public object? EditTextInactiveImageAsset
        {
            get => _editTextInactiveImageAsset;
            set { _editTextInactiveImageAsset = value; OnPropertyChanged(); }
        }

        public object? EditTextMouseOverImageAsset
        {
            get => _editTextMouseOverImageAsset;
            set { _editTextMouseOverImageAsset = value; OnPropertyChanged(); }
        }

        // Slider Properties
        private string _sliderImageMode = "SpriteSheet"; // "Texture" or "SpriteSheet"
        private string _sliderTrackBackgroundImage = string.Empty;
        private int _sliderTrackBackgroundFrame = 0;
        private object? _sliderTrackBackgroundImageAsset = null;
        private string _sliderTrackForegroundImage = string.Empty;
        private int _sliderTrackForegroundFrame = 0;
        private object? _sliderTrackForegroundImageAsset = null;
        private string _sliderThumbImage = string.Empty;
        private int _sliderThumbFrame = 0;
        private object? _sliderThumbImageAsset = null;
        private string _sliderMouseOverThumbImage = string.Empty;
        private int _sliderMouseOverThumbFrame = 0;
        private object? _sliderMouseOverThumbImageAsset = null;
        private string _sliderTickImage = string.Empty;
        private int _sliderTickFrame = 0;
        private object? _sliderTickImageAsset = null;
        private double _sliderMinimum = 0.0;
        private double _sliderMaximum = 1.0;  // Stride default is 1.0
        private double _sliderValue = 0.0;    // Stride default is 0.0
        private double _sliderStep = 0.1;     // Stride default is 0.1
        private double _sliderTickFrequency = 10.0;  // Stride default is 10.0
        private double _sliderTickOffset = 10.0;     // Stride default is 10.0
        private bool _areTicksDisplayed = false;
        private bool _shouldSnapToTicks = false;
        private bool _isDirectionReversed = false;
        private string _sliderOrientation = "Horizontal"; // Horizontal or Vertical
        private double _trackStartingOffsetLeft = 0.0;
        private double _trackStartingOffsetTop = 0.0;
        private double _trackStartingOffsetRight = 0.0;
        private double _trackStartingOffsetBottom = 0.0;

        public string SliderImageMode
        {
            get => _sliderImageMode;
            set { _sliderImageMode = value; OnPropertyChanged(); }
        }

        public string SliderTrackBackgroundImage
        {
            get => _sliderTrackBackgroundImage;
            set { _sliderTrackBackgroundImage = value; OnPropertyChanged(); }
        }

        public int SliderTrackBackgroundFrame
        {
            get => _sliderTrackBackgroundFrame;
            set { _sliderTrackBackgroundFrame = value; OnPropertyChanged(); }
        }

        public string SliderTrackForegroundImage
        {
            get => _sliderTrackForegroundImage;
            set { _sliderTrackForegroundImage = value; OnPropertyChanged(); }
        }

        public int SliderTrackForegroundFrame
        {
            get => _sliderTrackForegroundFrame;
            set { _sliderTrackForegroundFrame = value; OnPropertyChanged(); }
        }

        public string SliderThumbImage
        {
            get => _sliderThumbImage;
            set { _sliderThumbImage = value; OnPropertyChanged(); }
        }

        public int SliderThumbFrame
        {
            get => _sliderThumbFrame;
            set { _sliderThumbFrame = value; OnPropertyChanged(); }
        }

        public string SliderMouseOverThumbImage
        {
            get => _sliderMouseOverThumbImage;
            set { _sliderMouseOverThumbImage = value; OnPropertyChanged(); }
        }

        public int SliderMouseOverThumbFrame
        {
            get => _sliderMouseOverThumbFrame;
            set { _sliderMouseOverThumbFrame = value; OnPropertyChanged(); }
        }

        public string SliderTickImage
        {
            get => _sliderTickImage;
            set { _sliderTickImage = value; OnPropertyChanged(); }
        }

        public int SliderTickFrame
        {
            get => _sliderTickFrame;
            set { _sliderTickFrame = value; OnPropertyChanged(); }
        }

        public double SliderMinimum
        {
            get => _sliderMinimum;
            set { _sliderMinimum = value; OnPropertyChanged(); }
        }

        public double SliderMaximum
        {
            get => _sliderMaximum;
            set { _sliderMaximum = value; OnPropertyChanged(); }
        }

        public double SliderValue
        {
            get => _sliderValue;
            set { _sliderValue = Math.Clamp(value, _sliderMinimum, _sliderMaximum); OnPropertyChanged(); }
        }

        public double SliderStep
        {
            get => _sliderStep;
            set { _sliderStep = Math.Max(0, value); OnPropertyChanged(); }
        }

        public double SliderTickFrequency
        {
            get => _sliderTickFrequency;
            set { _sliderTickFrequency = value; OnPropertyChanged(); }
        }

        public double SliderTickOffset
        {
            get => _sliderTickOffset;
            set { _sliderTickOffset = value; OnPropertyChanged(); }
        }

        public bool AreTicksDisplayed
        {
            get => _areTicksDisplayed;
            set { _areTicksDisplayed = value; OnPropertyChanged(); }
        }

        public bool ShouldSnapToTicks
        {
            get => _shouldSnapToTicks;
            set { _shouldSnapToTicks = value; OnPropertyChanged(); }
        }

        public bool IsDirectionReversed
        {
            get => _isDirectionReversed;
            set { _isDirectionReversed = value; OnPropertyChanged(); }
        }

        public string SliderOrientation
        {
            get => _sliderOrientation;
            set { _sliderOrientation = value; OnPropertyChanged(); }
        }

        public double TrackStartingOffsetLeft
        {
            get => _trackStartingOffsetLeft;
            set { _trackStartingOffsetLeft = value; OnPropertyChanged(); }
        }

        public double TrackStartingOffsetTop
        {
            get => _trackStartingOffsetTop;
            set { _trackStartingOffsetTop = value; OnPropertyChanged(); }
        }

        public double TrackStartingOffsetRight
        {
            get => _trackStartingOffsetRight;
            set { _trackStartingOffsetRight = value; OnPropertyChanged(); }
        }

        public double TrackStartingOffsetBottom
        {
            get => _trackStartingOffsetBottom;
            set { _trackStartingOffsetBottom = value; OnPropertyChanged(); }
        }

        // Slider AssetReference properties (for export using toolkit helpers)
        public object? SliderTrackBackgroundImageAsset
        {
            get => _sliderTrackBackgroundImageAsset;
            set { _sliderTrackBackgroundImageAsset = value; OnPropertyChanged(); }
        }

        public object? SliderTrackForegroundImageAsset
        {
            get => _sliderTrackForegroundImageAsset;
            set { _sliderTrackForegroundImageAsset = value; OnPropertyChanged(); }
        }

        public object? SliderThumbImageAsset
        {
            get => _sliderThumbImageAsset;
            set { _sliderThumbImageAsset = value; OnPropertyChanged(); }
        }

        public object? SliderMouseOverThumbImageAsset
        {
            get => _sliderMouseOverThumbImageAsset;
            set { _sliderMouseOverThumbImageAsset = value; OnPropertyChanged(); }
        }

        public object? SliderTickImageAsset
        {
            get => _sliderTickImageAsset;
            set { _sliderTickImageAsset = value; OnPropertyChanged(); }
        }

        // ToggleButton Properties
        private string _toggleImageMode = "SpriteSheet"; // "Texture" or "SpriteSheet"
        private string _toggleCheckedImage = string.Empty;
        private int _toggleCheckedFrame = 0;
        private object? _toggleCheckedImageAsset = null;
        private string _toggleUncheckedImage = string.Empty;
        private int _toggleUncheckedFrame = 0;
        private object? _toggleUncheckedImageAsset = null;
        private string _toggleIndeterminateImage = string.Empty;
        private int _toggleIndeterminateFrame = 0;
        private object? _toggleIndeterminateImageAsset = null;
        private string _toggleState = "UnChecked"; // "Checked", "UnChecked", "Indeterminate"
        private bool _isThreeState = false;
        private string _toggleClickMode = "Release"; // "Release" or "Press"

        public string ToggleImageMode
        {
            get => _toggleImageMode;
            set { _toggleImageMode = value; OnPropertyChanged(); }
        }

        public string ToggleCheckedImage
        {
            get => _toggleCheckedImage;
            set { _toggleCheckedImage = value; OnPropertyChanged(); }
        }

        public int ToggleCheckedFrame
        {
            get => _toggleCheckedFrame;
            set { _toggleCheckedFrame = value; OnPropertyChanged(); }
        }

        public string ToggleUncheckedImage
        {
            get => _toggleUncheckedImage;
            set { _toggleUncheckedImage = value; OnPropertyChanged(); }
        }

        public int ToggleUncheckedFrame
        {
            get => _toggleUncheckedFrame;
            set { _toggleUncheckedFrame = value; OnPropertyChanged(); }
        }

        public string ToggleIndeterminateImage
        {
            get => _toggleIndeterminateImage;
            set { _toggleIndeterminateImage = value; OnPropertyChanged(); }
        }

        public int ToggleIndeterminateFrame
        {
            get => _toggleIndeterminateFrame;
            set { _toggleIndeterminateFrame = value; OnPropertyChanged(); }
        }

        public string ToggleState
        {
            get => _toggleState;
            set { _toggleState = value; OnPropertyChanged(); }
        }

        public bool IsThreeState
        {
            get => _isThreeState;
            set { _isThreeState = value; OnPropertyChanged(); }
        }

        public string ToggleClickMode
        {
            get => _toggleClickMode;
            set { _toggleClickMode = value; OnPropertyChanged(); }
        }

        // ToggleButton AssetReference properties (for export using toolkit helpers)
        public object? ToggleCheckedImageAsset
        {
            get => _toggleCheckedImageAsset;
            set { _toggleCheckedImageAsset = value; OnPropertyChanged(); }
        }

        public object? ToggleUncheckedImageAsset
        {
            get => _toggleUncheckedImageAsset;
            set { _toggleUncheckedImageAsset = value; OnPropertyChanged(); }
        }

        public object? ToggleIndeterminateImageAsset
        {
            get => _toggleIndeterminateImageAsset;
            set { _toggleIndeterminateImageAsset = value; OnPropertyChanged(); }
        }

        // ModalElement Properties
        private bool _isModal = true;
        private Color _overlayColor = Color.FromArgb(128, 0, 0, 0); // Semi-transparent black

        public bool IsModal
        {
            get => _isModal;
            set { _isModal = value; OnPropertyChanged(); }
        }

        public Color OverlayColor
        {
            get => _overlayColor;
            set { _overlayColor = value; OnPropertyChanged(); }
        }

        // ContentDecorator Properties (Border, etc.)
        private string _contentDecoratorImageMode = "SpriteSheet"; // "Texture" or "SpriteSheet"
        private string _backgroundImageSource = string.Empty;
        private int _backgroundImageFrame = 0;
        private object? _backgroundImageAsset = null;
        private Color _borderColor = Colors.Transparent;
        private double _borderThicknessLeft = 0.0;
        private double _borderThicknessTop = 0.0;
        private double _borderThicknessRight = 0.0;
        private double _borderThicknessBottom = 0.0;

        public string ContentDecoratorImageMode
        {
            get => _contentDecoratorImageMode;
            set { _contentDecoratorImageMode = value; OnPropertyChanged(); }
        }

        public string BackgroundImageSource
        {
            get => _backgroundImageSource;
            set { _backgroundImageSource = value; OnPropertyChanged(); }
        }

        public int BackgroundImageFrame
        {
            get => _backgroundImageFrame;
            set { _backgroundImageFrame = value; OnPropertyChanged(); }
        }

        public object? BackgroundImageAsset
        {
            get => _backgroundImageAsset;
            set { _backgroundImageAsset = value; OnPropertyChanged(); }
        }

        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; OnPropertyChanged(); }
        }

        public double BorderThicknessLeft
        {
            get => _borderThicknessLeft;
            set { _borderThicknessLeft = Math.Max(0, value); OnPropertyChanged(); }
        }

        public double BorderThicknessTop
        {
            get => _borderThicknessTop;
            set { _borderThicknessTop = Math.Max(0, value); OnPropertyChanged(); }
        }

        public double BorderThicknessRight
        {
            get => _borderThicknessRight;
            set { _borderThicknessRight = Math.Max(0, value); OnPropertyChanged(); }
        }

        public double BorderThicknessBottom
        {
            get => _borderThicknessBottom;
            set { _borderThicknessBottom = Math.Max(0, value); OnPropertyChanged(); }
        }

        // UniformGrid Properties
        private int _uniformGridRows = 1;
        private int _uniformGridColumns = 1;

        public int UniformGridRows
        {
            get => _uniformGridRows;
            set { _uniformGridRows = Math.Max(1, value); OnPropertyChanged(); }
        }

        public int UniformGridColumns
        {
            get => _uniformGridColumns;
            set { _uniformGridColumns = Math.Max(1, value); OnPropertyChanged(); }
        }

        public UIElementViewModel? Parent { get; set; }

        public ObservableCollection<UIElementViewModel> Children { get; set; }

        public UIElementViewModel(string name, string elementType)
        {
            Name = name;
            ElementType = elementType;

            // Initialize Children collection and subscribe to changes
            Children = new ObservableCollection<UIElementViewModel>();
            Children.CollectionChanged += Children_CollectionChanged;

            // Set element-specific defaults
            InitializeDefaultsByType();
        }

        private void Children_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Handle added items
            if (e.NewItems != null)
            {
                foreach (UIElementViewModel child in e.NewItems)
                {
                    child.Parent = this;
                }
            }

            // Handle removed items
            if (e.OldItems != null)
            {
                foreach (UIElementViewModel child in e.OldItems)
                {
                    child.Parent = null;
                }
            }
        }

        private void InitializeDefaultsByType()
        {
            switch (ElementType)
            {
                case "Button":
                    Width = 200;
                    Height = 100;
                    break;
                case "TextBlock":
                    Text = "Text";
                    break;
                case "ImageElement":
                    ImageSource = "(None)";
                    Width = 200;
                    Height = 200;
                    break;
            }
        }

        public bool IsDescendantOf(UIElementViewModel element)
        {
            var current = Parent;
            while (current != null)
            {
                if (current == element)
                {
                    return true;
                }
                current = current.Parent;
            }
            return false;
        }

        /// <summary>
        /// Get the absolute position of this element in artboard coordinates.
        /// For nested elements, this accumulates the position of all ancestors.
        /// </summary>
        public (double X, double Y) GetAbsolutePosition()
        {
            double absX = X;
            double absY = Y;

            var current = Parent;
            while (current != null && !current.IsSystemElement)
            {
                absX += current.X;
                absY += current.Y;
                current = current.Parent;
            }

            return (absX, absY);
        }

        /// <summary>
        /// Creates a deep clone of this element with a new ID and name.
        /// Does not clone children - only the element itself.
        /// </summary>
        public UIElementViewModel Clone(string newName)
        {
            var clone = new UIElementViewModel(newName, ElementType)
            {
                // Layout
                X = X,
                Y = Y,
                Width = Width,
                Height = Height,
                HorizontalAlignment = HorizontalAlignment,
                VerticalAlignment = VerticalAlignment,
                MarginLeft = MarginLeft,
                MarginTop = MarginTop,
                MarginRight = MarginRight,
                MarginBottom = MarginBottom,

                // Common Appearance
                BackgroundColor = BackgroundColor,
                Opacity = Opacity,
                DrawLayerNumber = DrawLayerNumber,
                ClipToBounds = ClipToBounds,
                IsEnabled = IsEnabled,
                CanBeHitByUser = CanBeHitByUser,
                Visibility = Visibility,

                // Editor-specific
                IsLocked = IsLocked,
                AllowCanvasOverflow = AllowCanvasOverflow,

                // ImageElement
                ImageSource = ImageSource,
                ImageAssetReference = ImageAssetReference,
                ImageAssetType = ImageAssetType,
                SpriteFrame = SpriteFrame,
                ImageTintColor = ImageTintColor,
                StretchType = StretchType,
                StretchDirection = StretchDirection,

                // TextBlock
                Text = Text,
                FontFamily = FontFamily,
                FontAssetReference = FontAssetReference,
                FontSize = FontSize,
                TextColor = TextColor,
                TextAlignment = TextAlignment,
                WrapText = WrapText,
                DoNotSnapText = DoNotSnapText,

                // Button/ToggleButton
                ButtonText = ButtonText,
                ClickMode = ClickMode,
                ButtonImageMode = ButtonImageMode,
                ButtonNotPressedImage = ButtonNotPressedImage,
                ButtonNotPressedFrame = ButtonNotPressedFrame,
                ButtonNotPressedImageAsset = ButtonNotPressedImageAsset,
                ButtonPressedImage = ButtonPressedImage,
                ButtonPressedFrame = ButtonPressedFrame,
                ButtonPressedImageAsset = ButtonPressedImageAsset,
                ButtonMouseOverImage = ButtonMouseOverImage,
                ButtonMouseOverFrame = ButtonMouseOverFrame,
                ButtonMouseOverImageAsset = ButtonMouseOverImageAsset,

                // Grid
                RowDefinitions = RowDefinitions,
                ColumnDefinitions = ColumnDefinitions,
                RowSpacing = RowSpacing,
                ColumnSpacing = ColumnSpacing,

                // Grid Attachment
                GridRow = GridRow,
                GridColumn = GridColumn,
                GridRowSpan = GridRowSpan,
                GridColumnSpan = GridColumnSpan,

                // ScrollViewer
                ScrollMode = ScrollMode,
                ScrollBarThickness = ScrollBarThickness,
                TouchScrollingEnabled = TouchScrollingEnabled,
                ScrollBarColor = ScrollBarColor,
                ScrollingSpeed = ScrollingSpeed,
                ScrollStartThreshold = ScrollStartThreshold,

                // Slider
                SliderOrientation = SliderOrientation,
                SliderMinimum = SliderMinimum,
                SliderMaximum = SliderMaximum,
                SliderValue = SliderValue,
                SliderStep = SliderStep,
                SliderTickFrequency = SliderTickFrequency,
                SliderTickOffset = SliderTickOffset,
                SliderImageMode = SliderImageMode,
                SliderTrackBackgroundImage = SliderTrackBackgroundImage,
                SliderTrackBackgroundFrame = SliderTrackBackgroundFrame,
                SliderTrackBackgroundImageAsset = SliderTrackBackgroundImageAsset,
                SliderTrackForegroundImage = SliderTrackForegroundImage,
                SliderTrackForegroundFrame = SliderTrackForegroundFrame,
                SliderTrackForegroundImageAsset = SliderTrackForegroundImageAsset,
                SliderThumbImage = SliderThumbImage,
                SliderThumbFrame = SliderThumbFrame,
                SliderThumbImageAsset = SliderThumbImageAsset,
                SliderMouseOverThumbImage = SliderMouseOverThumbImage,
                SliderMouseOverThumbFrame = SliderMouseOverThumbFrame,
                SliderMouseOverThumbImageAsset = SliderMouseOverThumbImageAsset,
                SliderTickImage = SliderTickImage,
                SliderTickFrame = SliderTickFrame,
                SliderTickImageAsset = SliderTickImageAsset,

                // StackPanel
                StackPanelOrientation = StackPanelOrientation,

                // UniformGrid
                UniformGridRows = UniformGridRows,
                UniformGridColumns = UniformGridColumns
            };

            return clone;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

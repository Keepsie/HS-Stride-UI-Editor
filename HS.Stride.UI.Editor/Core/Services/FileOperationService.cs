// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using HS.Stride.UI.Editor.Core.Services.Interfaces;
using HS.Stride.UI.Editor.ViewModels;
using HS.Stride.Editor.Toolkit.Core;
using HS.Stride.Editor.Toolkit.Core.AssetEditing;
using HS.Stride.Editor.Toolkit.Core.UIPageEditing;
using ToolkitUIElement = HS.Stride.Editor.Toolkit.Core.UIPageEditing.UIElement;

namespace HS.Stride.UI.Editor.Core.Services
{
    /// <summary>
    /// Handles all file operations: New, Open, Save, and UIPage ↔ ViewModel conversion
    /// </summary>
    public class FileOperationService : IFileOperationService
    {
        private StrideProject? _connectedProject;
        private IAssetService? _assetService;
        private UIPage? _currentUIPage;
        private string? _currentFilePath;

        public string? CurrentFilePath => _currentFilePath;
        public bool HasUnsavedChanges { get; set; }

        /// <summary>
        /// Exposes the current UIPage for debugging purposes
        /// </summary>
        public UIPage? CurrentUIPage => _currentUIPage;

        /// <summary>
        /// Set the connected Stride project (required for all operations)
        /// </summary>
        public void SetProject(StrideProject? project)
        {
            _connectedProject = project;
        }

        /// <summary>
        /// Set the asset service for font and image loading
        /// </summary>
        public void SetAssetService(IAssetService? assetService)
        {
            _assetService = assetService;
        }

        #region New Document

        /// <summary>
        /// Create a new blank UI page with root Grid container
        /// </summary>
        public List<UIElementViewModel> CreateNewDocument(double designWidth, double designHeight)
        {
            var rootElements = new List<UIElementViewModel>();

            // Create hidden root Grid container (like Stride's default structure)
            var rootGrid = new UIElementViewModel("RootGrid", "Grid")
            {
                Width = designWidth,
                Height = designHeight,
                X = 0,
                Y = 0,
                IsSystemElement = true // Hidden from hierarchy
            };
            rootElements.Add(rootGrid);

            // Reset current file state
            _currentUIPage = null;
            _currentFilePath = null;
            HasUnsavedChanges = false;

            return rootElements;
        }

        #endregion

        #region Load Document

        /// <summary>
        /// Load a UI page from file
        /// </summary>
        public LoadDocumentResult LoadDocument(string filePath, double defaultDesignWidth, double defaultDesignHeight)
        {
            if (_connectedProject == null)
                throw new InvalidOperationException("No project connected. Use SetProject() first.");

            // Load via toolkit
            _currentUIPage = UIPage.Load(filePath);
            _currentFilePath = filePath;

            // Get design resolution from the UIPage (v1.6.0+)
            var resolution = _currentUIPage.Resolution;
            double designWidth = resolution.TryGetValue("X", out var w) ? w : 1280;
            double designHeight = resolution.TryGetValue("Y", out var h) ? h : 720;

            // Convert to ViewModels
            var rootElements = ConvertUIPageToViewModels(_currentUIPage);

            HasUnsavedChanges = false;

            return new LoadDocumentResult(rootElements, designWidth, designHeight);
        }

        /// <summary>
        /// Convert a toolkit UIPage to ViewModels
        /// </summary>
        private List<UIElementViewModel> ConvertUIPageToViewModels(UIPage uiPage)
        {
            var rootElements = new List<UIElementViewModel>();
            bool isFirstRoot = true;

            foreach (var rootElement in uiPage.RootElements)
            {
                var rootVM = ConvertToolkitElementToViewModel(rootElement);

                // Mark the first root Grid as a system element (hidden container like Stride's default)
                if (isFirstRoot && rootVM.ElementType == "Grid")
                {
                    rootVM.IsSystemElement = true;
                    isFirstRoot = false;
                }

                rootElements.Add(rootVM);
            }

            // After full hierarchy is built, recalculate positions for all elements
            // This ensures alignment-based positioning works correctly now that Parent references are set
            foreach (var root in rootElements)
            {
                RecalculatePositionsRecursive(root);
            }

            // Recalculate Z-Index for all elements based on hierarchy order
            // Stride often has all elements at the same Z-Index (0), which causes display issues
            // when only some elements are edited. Force proper Z-order on load to fix this.
            for (int i = 0; i < rootElements.Count; i++)
            {
                rootElements[i].ZIndex = i;
                RecalculateZIndicesRecursive(rootElements[i]);
            }

            return rootElements;
        }

        /// <summary>
        /// Recursively recalculate positions for all elements after hierarchy is built
        /// </summary>
        private void RecalculatePositionsRecursive(UIElementViewModel element)
        {
            // Recalculate this element's position based on alignment and margin
            element.RecalculatePositionFromAlignment();

            // Recursively process children
            foreach (var child in element.Children)
            {
                RecalculatePositionsRecursive(child);
            }
        }

        /// <summary>
        /// Recursively set Z-Index for all children based on their position in the Children collection.
        /// This ensures proper draw order when loading pages where Stride has all elements at Z-Index 0.
        /// </summary>
        private void RecalculateZIndicesRecursive(UIElementViewModel parent)
        {
            for (int i = 0; i < parent.Children.Count; i++)
            {
                parent.Children[i].ZIndex = i;
                RecalculateZIndicesRecursive(parent.Children[i]);
            }
        }

        /// <summary>
        /// Recursively convert a toolkit UIElement to ViewModel
        /// </summary>
        private UIElementViewModel ConvertToolkitElementToViewModel(ToolkitUIElement toolkitElement)
        {
            var vm = new UIElementViewModel(toolkitElement.Name, toolkitElement.Type);

            // Get alignment first - needed for position calculation (use toolkit helpers)
            var hAlign = toolkitElement.GetHorizontalAlignment();
            var vAlign = toolkitElement.GetVerticalAlignment();

            // Get margin values
            var margin = toolkitElement.GetMargin();

            // Get size using toolkit helpers (handles string-to-float conversion properly)
            var width = toolkitElement.GetWidth();
            var height = toolkitElement.GetHeight();

            // Get parent dimensions for position calculation
            var (parentWidth, parentHeight) = toolkitElement.GetParentDimensions();
            var parentType = toolkitElement.Parent?.Type ?? "Grid"; // Root elements parent to Grid

            // For TextBlock without explicit size, measure text to get actual content size
            // This must happen BEFORE position calculation so centering uses correct dimensions
            if (toolkitElement.Type == "TextBlock" && !width.HasValue && !height.HasValue)
            {
                var text = toolkitElement.GetText() ?? "";
                var fontSize = toolkitElement.GetFontSize(); // Uses helper - defaults to 20f
                var (measuredWidth, measuredHeight) = MeasureText(text, fontSize);

                // Add padding to account for font metric differences between WPF and Stride
                // Stride uses different font rendering, so we need extra space to prevent clipping
                const float TEXT_PADDING_X = 8f;  // Extra width padding
                const float TEXT_PADDING_Y = 4f;  // Extra height padding

                // Use measured size + padding (Stride auto-sizes TextBlocks to fit content)
                width = (float)measuredWidth + TEXT_PADDING_X;
                height = (float)measuredHeight + TEXT_PADDING_Y;
            }

            // Calculate actual X/Y position based on alignment type AND parent type
            // Canvas children use margin as absolute position (alignment is ignored for positioning)
            var (x, y, w, h) = CalculatePositionFromAlignment(
                hAlign, vAlign, margin, width, height, parentWidth, parentHeight, parentType);

            // Set position and size without triggering margin updates (would overwrite loaded margins)
            vm.SetPositionWithoutMarginUpdate(x, y);
            vm.Width = w;
            vm.Height = h;

            // Store margin values (used for alignment-based position recalculation)
            // Use SetMarginWithoutRecalculation to avoid triggering recalc during load
            vm.SetMarginWithoutRecalculation(margin.Left, margin.Top, margin.Right, margin.Bottom);

            // Store alignment - note: setters will try to recalculate position but Parent isn't set yet
            // so recalculation will be skipped. Position is already set correctly above.
            vm.HorizontalAlignment = hAlign;
            vm.VerticalAlignment = vAlign;

            // Common appearance properties - use toolkit helpers for proper type conversion
            vm.Opacity = toolkitElement.GetOpacity();
            vm.DrawLayerNumber = toolkitElement.GetDrawLayer();

            // Load ZIndex using toolkit helper
            vm.ZIndex = toolkitElement.GetZIndex();

            // ClipToBounds - no toolkit getter available
            vm.ClipToBounds = toolkitElement.Get<bool?>("ClipToBounds") ?? false;

            // Behavior properties - use toolkit helpers
            vm.Visibility = toolkitElement.Get<string>("Visibility") ?? "Visible"; // No string getter, only IsVisible() bool
            vm.IsEnabled = toolkitElement.GetIsEnabled();
            vm.CanBeHitByUser = toolkitElement.GetCanBeHitByUser();

            // BackgroundColor (RGBA 0-255) - use toolkit helper
            var bgColor = toolkitElement.GetBackgroundColor();
            vm.BackgroundColor = Color.FromArgb((byte)bgColor.A, (byte)bgColor.R, (byte)bgColor.G, (byte)bgColor.B);

            // Type-specific properties
            switch (toolkitElement.Type)
            {
                case "TextBlock":
                    LoadTextBlockProperties(vm, toolkitElement);
                    // Trust Stride's saved dimensions - don't auto-expand based on WPF text measurement
                    // WPF and Stride have different font metrics, so WPF measurement is not accurate
                    break;

                case "Button":
                    LoadButtonProperties(vm, toolkitElement);
                    break;

                case "ImageElement":
                    LoadImageElementProperties(vm, toolkitElement);
                    break;

                case "StackPanel":
                    LoadStackPanelProperties(vm, toolkitElement);
                    break;

                case "ScrollViewer":
                    LoadScrollViewerProperties(vm, toolkitElement);
                    break;

                case "EditText":
                    LoadEditTextProperties(vm, toolkitElement);
                    break;

                case "Slider":
                    LoadSliderProperties(vm, toolkitElement);
                    break;

                case "ToggleButton":
                    LoadToggleButtonProperties(vm, toolkitElement);
                    break;

                case "ModalElement":
                    LoadModalElementProperties(vm, toolkitElement);
                    break;

                case "UniformGrid":
                    LoadUniformGridProperties(vm, toolkitElement);
                    break;

                case "ContentDecorator":
                case "Border":
                    LoadContentDecoratorProperties(vm, toolkitElement);
                    break;
            }

            // Recursively convert children, sorted by ZIndex to preserve draw order
            // This matches how Stride sorts VisualChildrenCollection when rendering
            var children = toolkitElement.GetChildren()
                .OrderBy(c => c.GetZIndex())
                .ToList();

            foreach (var child in children)
            {
                var childVM = ConvertToolkitElementToViewModel(child);
                childVM.Parent = vm;
                vm.Children.Add(childVM);
            }

            return vm;
        }

        /// <summary>
        /// Calculate actual X/Y position and size based on Stride's alignment system.
        /// Stride uses different margin semantics depending on alignment:
        /// - Left/Top alignment: margin.Left/Top = absolute X/Y position
        /// - Right/Bottom alignment: margin.Right/Bottom = distance from opposite edge
        /// - Center alignment: margins offset from center
        /// - Stretch alignment: margins define distance from all edges
        ///
        /// IMPORTANT: Canvas children use margin as ABSOLUTE position (alignment is ignored for positioning).
        /// This matches Stride's Canvas behavior where AbsolutePosition (from margin) determines position.
        /// </summary>
        private (double X, double Y, double Width, double Height) CalculatePositionFromAlignment(
            string hAlign, string vAlign,
            (float Left, float Top, float Right, float Bottom) margin,
            float? width, float? height,
            float parentWidth, float parentHeight,
            string parentType = "Grid")
        {
            double x, y, w, h;

            // For elements without explicit size, use parent-relative defaults
            // This ensures elements stay within bounds even when parent is small
            const float MIN_SIZE = 20f;
            const float MAX_DEFAULT_SIZE = 100f;

            // Canvas children: margin IS the absolute position, alignment is ignored for positioning
            // This matches Stride's Canvas.ArrangeOverride which uses AbsolutePosition (margin) directly
            if (parentType == "Canvas")
            {
                x = margin.Left;
                y = margin.Top;
                w = width ?? Math.Min(MAX_DEFAULT_SIZE, Math.Max(MIN_SIZE, parentWidth * 0.8f));
                h = height ?? Math.Min(MAX_DEFAULT_SIZE, Math.Max(MIN_SIZE, parentHeight * 0.8f));
                return (x, y, w, h);
            }

            // Calculate width and X position based on horizontal alignment
            switch (hAlign)
            {
                case "Left":
                    x = margin.Left;
                    w = width ?? Math.Min(MAX_DEFAULT_SIZE, Math.Max(MIN_SIZE, parentWidth * 0.8f));
                    break;

                case "Right":
                    w = width ?? Math.Min(MAX_DEFAULT_SIZE, Math.Max(MIN_SIZE, parentWidth * 0.8f));
                    x = parentWidth - margin.Right - w;
                    break;

                case "Center":
                    if (width.HasValue)
                    {
                        // Explicit size: center the element within parent, adjusted by margins
                        w = width.Value;
                    }
                    else
                    {
                        // Auto-sized: use majority of parent width so it appears centered
                        // For text elements, this gives room for content while staying visible
                        w = Math.Min(MAX_DEFAULT_SIZE, Math.Max(MIN_SIZE, parentWidth * 0.8f));
                    }
                    // Stride formula: offsets.X = margin.Left + (parentWidth - (w + margin.Left + margin.Right)) / 2
                    x = margin.Left + (parentWidth - w - margin.Left - margin.Right) / 2;
                    break;

                case "Stretch":
                default:
                    x = margin.Left;
                    if (width.HasValue)
                    {
                        w = width.Value;
                    }
                    else
                    {
                        // Element stretches between margins
                        w = parentWidth - margin.Left - margin.Right;
                        if (w < MIN_SIZE) w = Math.Min(MAX_DEFAULT_SIZE, parentWidth * 0.8f);
                    }
                    break;
            }

            // Calculate height and Y position based on vertical alignment
            switch (vAlign)
            {
                case "Top":
                    y = margin.Top;
                    h = height ?? Math.Min(MAX_DEFAULT_SIZE, Math.Max(MIN_SIZE, parentHeight * 0.8f));
                    break;

                case "Bottom":
                    h = height ?? Math.Min(MAX_DEFAULT_SIZE, Math.Max(MIN_SIZE, parentHeight * 0.8f));
                    y = parentHeight - margin.Bottom - h;
                    break;

                case "Center":
                    if (height.HasValue)
                    {
                        // Explicit size: center the element within parent, adjusted by margins
                        h = height.Value;
                    }
                    else
                    {
                        // Auto-sized: use majority of parent height so it appears centered
                        h = Math.Min(MAX_DEFAULT_SIZE, Math.Max(MIN_SIZE, parentHeight * 0.8f));
                    }
                    // Stride formula: offsets.Y = margin.Top + (parentHeight - (h + margin.Top + margin.Bottom)) / 2
                    y = margin.Top + (parentHeight - h - margin.Top - margin.Bottom) / 2;
                    break;

                case "Stretch":
                default:
                    y = margin.Top;
                    if (height.HasValue)
                    {
                        h = height.Value;
                    }
                    else
                    {
                        // Element stretches between margins
                        h = parentHeight - margin.Top - margin.Bottom;
                        if (h < MIN_SIZE) h = Math.Min(MAX_DEFAULT_SIZE, parentHeight * 0.8f);
                    }
                    break;
            }

            return (x, y, w, h);
        }

        /// <summary>
        /// Strip surrounding quotes from a string value if present.
        /// Some YAML parsers may return quoted strings that need to be cleaned up.
        /// </summary>
        private string CleanTextValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var result = value;

            // Strip surrounding single quotes
            if (result.StartsWith("'") && result.EndsWith("'") && result.Length > 1)
                result = result.Substring(1, result.Length - 2);

            // Strip surrounding double quotes
            if (result.StartsWith("\"") && result.EndsWith("\"") && result.Length > 1)
                result = result.Substring(1, result.Length - 2);

            return result;
        }

        /// <summary>
        /// Clean asset reference strings that may have been corrupted with extra quotes.
        /// Strips all quote characters from the string to get the raw guid:path format.
        /// </summary>
        private string CleanAssetReference(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Remove all quote characters (both escaped and unescaped)
            var result = value.Replace("\\\"", "").Replace("\"", "").Replace("'", "");

            return result;
        }

        private void LoadTextBlockProperties(UIElementViewModel vm, ToolkitUIElement element)
        {
            vm.Text = CleanTextValue(element.GetText());
            vm.FontSize = element.GetFontSize(); // Uses helper - defaults to 20f if not set
            vm.TextAlignment = element.Get<string>("TextAlignment") ?? "Left"; // No toolkit getter
            vm.WrapText = element.GetWrapText();
            vm.DoNotSnapText = element.GetDoNotSnapText();

            // Text color - use toolkit helper
            var textColor = element.GetTextColor();
            vm.TextColor = Color.FromArgb((byte)textColor.A, (byte)textColor.R, (byte)textColor.G, (byte)textColor.B);

            // Outline color and thickness - parse from dictionary property
            if (element.Properties.TryGetValue("OutlineColor", out var outlineObj) && outlineObj is Dictionary<string, object> outlineDict)
            {
                var outR = outlineDict.TryGetValue("R", out var rv) ? Convert.ToByte(rv) : (byte)0;
                var outG = outlineDict.TryGetValue("G", out var gv) ? Convert.ToByte(gv) : (byte)0;
                var outB = outlineDict.TryGetValue("B", out var bv) ? Convert.ToByte(bv) : (byte)0;
                var outA = outlineDict.TryGetValue("A", out var av) ? Convert.ToByte(av) : (byte)255;
                vm.TextOutlineColor = Color.FromArgb(outA, outR, outG, outB);
            }
            vm.TextOutlineThickness = element.Get<float?>("OutlineThickness") ?? 0f;

            // Font asset reference - load the actual font from the project
            var fontRef = element.GetFont();
            if (!string.IsNullOrEmpty(fontRef) && _assetService != null)
            {
                // Clean up any quotes that may have been incorrectly added
                vm.FontAssetReference = CleanAssetReference(fontRef);
                var fontInfo = _assetService.GetFontInfo(vm.FontAssetReference);
                if (fontInfo != null)
                {
                    vm.FontFamily = fontInfo.FontFamilyName;
                }
            }
        }

        private void LoadButtonProperties(UIElementViewModel vm, ToolkitUIElement element)
        {
            // Button text comes from Content child TextBlock
            var contentElement = element.GetContent();
            var buttonText = contentElement != null ? CleanTextValue(contentElement.GetText()) : null;
            vm.ButtonText = string.IsNullOrEmpty(buttonText) ? "Button" : buttonText;

            // Click mode - use toolkit helper
            vm.ClickMode = element.GetClickMode();

            // NotPressedImage - check sprite sheet first, then texture
            if (element.IsSpriteFromSheet("NotPressedImage"))
            {
                var sprite = element.GetNotPressedImageSprite();
                if (sprite.HasValue)
                {
                    vm.ButtonNotPressedImage = sprite.Value.AssetReference ?? "";
                    vm.ButtonNotPressedFrame = sprite.Value.Frame;
                }
            }
            else if (element.IsSpriteFromTexture("NotPressedImage"))
            {
                vm.ButtonNotPressedImage = element.GetNotPressedImageTexture() ?? "";
                vm.ButtonNotPressedFrame = 0;
            }

            // PressedImage - check sprite sheet first, then texture
            if (element.IsSpriteFromSheet("PressedImage"))
            {
                var sprite = element.GetPressedImageSprite();
                if (sprite.HasValue)
                {
                    vm.ButtonPressedImage = sprite.Value.AssetReference ?? "";
                    vm.ButtonPressedFrame = sprite.Value.Frame;
                }
            }
            else if (element.IsSpriteFromTexture("PressedImage"))
            {
                vm.ButtonPressedImage = element.GetPressedImageTexture() ?? "";
                vm.ButtonPressedFrame = 0;
            }

            // MouseOverImage - check sprite sheet first, then texture
            if (element.IsSpriteFromSheet("MouseOverImage"))
            {
                var sprite = element.GetMouseOverImageSprite();
                if (sprite.HasValue)
                {
                    vm.ButtonMouseOverImage = sprite.Value.AssetReference ?? "";
                    vm.ButtonMouseOverFrame = sprite.Value.Frame;
                }
            }
            else if (element.IsSpriteFromTexture("MouseOverImage"))
            {
                vm.ButtonMouseOverImage = element.GetMouseOverImageTexture() ?? "";
                vm.ButtonMouseOverFrame = 0;
            }
        }

        private void LoadImageElementProperties(UIElementViewModel vm, ToolkitUIElement element)
        {
            // Check if Source has a sprite set using helper methods (v1.6.0+)
            if (element.IsSpriteFromSheet("Source"))
            {
                // GetSpriteSheet returns a tuple (string? AssetReference, int Frame)?
                var sprite = element.GetSpriteSheet("Source");
                if (sprite.HasValue)
                {
                    vm.ImageSource = sprite.Value.AssetReference ?? "(None)";
                    vm.ImageAssetType = "SpriteSheet";
                    vm.SpriteFrame = sprite.Value.Frame;
                    // Note: We store the string reference here, not the AssetReference object
                    // The AssetReference will be set when user selects via PropertyPanel
                }
            }
            else if (element.IsSpriteFromTexture("Source"))
            {
                var textureRef = element.GetTextureSource("Source");
                vm.ImageSource = textureRef ?? "(None)";
                vm.ImageAssetType = "Texture";
                vm.SpriteFrame = 0;
            }
            else
            {
                vm.ImageSource = "(None)";
                vm.SpriteFrame = 0;
            }

            // Tint color using helper (v1.6.0+)
            var color = element.GetColor();
            vm.ImageTintColor = Color.FromArgb((byte)color.A, (byte)color.R, (byte)color.G, (byte)color.B);

            // Stretch properties - use toolkit helpers
            vm.StretchType = element.GetStretchType();
            vm.StretchDirection = element.GetStretchDirection();
        }

        private void LoadStackPanelProperties(UIElementViewModel vm, ToolkitUIElement element)
        {
            vm.StackPanelOrientation = element.GetOrientation();
            vm.ItemVirtualizationEnabled = element.Get<bool?>("ItemVirtualizationEnabled") ?? false; // No toolkit getter
        }

        private void LoadScrollViewerProperties(UIElementViewModel vm, ToolkitUIElement element)
        {
            vm.ScrollMode = element.GetScrollMode();
            vm.ScrollBarThickness = element.GetScrollBarThickness();
            vm.ScrollingSpeed = element.GetScrollingSpeed();
            vm.Deceleration = element.GetDeceleration();
            vm.ScrollStartThreshold = element.GetScrollStartThreshold();
            vm.TouchScrollingEnabled = element.Get<bool?>("TouchScrollingEnabled") ?? true; // No toolkit getter
            vm.SnapToAnchors = element.Get<bool?>("SnapToAnchors") ?? false; // No toolkit getter

            // ScrollBarColor - no toolkit getter available
            var r = element.Get<byte?>("ScrollBarColor.R") ?? 26;   // Stride default Color(0.1f, 0.1f, 0.1f)
            var g = element.Get<byte?>("ScrollBarColor.G") ?? 26;
            var b = element.Get<byte?>("ScrollBarColor.B") ?? 26;
            var a = element.Get<byte?>("ScrollBarColor.A") ?? 255;
            vm.ScrollBarColor = Color.FromArgb(a, r, g, b);
        }

        private void LoadEditTextProperties(UIElementViewModel vm, ToolkitUIElement element)
        {
            vm.Text = CleanTextValue(element.GetText());
            vm.FontSize = element.GetFontSize(); // Uses helper - defaults to 20f

            // Text color - no toolkit getter available for EditText TextColor
            var r = element.Get<byte?>("TextColor.R") ?? 240;
            var g = element.Get<byte?>("TextColor.G") ?? 240;
            var b = element.Get<byte?>("TextColor.B") ?? 240;
            var a = element.Get<byte?>("TextColor.A") ?? 255;
            vm.TextColor = Color.FromArgb(a, r, g, b);

            vm.MaxLength = element.GetMaxLength();
            vm.IsReadOnly = element.GetIsReadOnly();
            vm.InputType = element.GetInputType();
            vm.MinLines = element.GetMinLines();
            vm.MaxLines = element.GetMaxLines();
            vm.CaretWidth = element.Get<float?>("CaretWidth") ?? 1.0f; // No toolkit getter
            vm.CaretFrequency = element.GetCaretFrequency();

            // Caret color - dot syntax (v1.6.0+)
            var cR = element.Get<byte?>("CaretColor.R") ?? 240;
            var cG = element.Get<byte?>("CaretColor.G") ?? 240;
            var cB = element.Get<byte?>("CaretColor.B") ?? 240;
            var cA = element.Get<byte?>("CaretColor.A") ?? 255;
            vm.CaretColor = Color.FromArgb(cA, cR, cG, cB);

            // Selection color - dot syntax (v1.6.0+)
            var sR = element.Get<byte?>("SelectionColor.R") ?? 51;
            var sG = element.Get<byte?>("SelectionColor.G") ?? 153;
            var sB = element.Get<byte?>("SelectionColor.B") ?? 255;
            var sA = element.Get<byte?>("SelectionColor.A") ?? 255;
            vm.SelectionColor = Color.FromArgb(sA, sR, sG, sB);

            // IME Selection color - dot syntax (v1.6.0+)
            var iR = element.Get<byte?>("IMESelectionColor.R") ?? 240;
            var iG = element.Get<byte?>("IMESelectionColor.G") ?? 255;
            var iB = element.Get<byte?>("IMESelectionColor.B") ?? 240;
            var iA = element.Get<byte?>("IMESelectionColor.A") ?? 255;
            vm.IMESelectionColor = Color.FromArgb(iA, iR, iG, iB);

            // 3-state images - use proper toolkit helpers
            // ActiveImage
            if (element.IsSpriteFromSheet("ActiveImage"))
            {
                var sprite = element.GetActiveImageSprite();
                if (sprite.HasValue)
                {
                    vm.EditTextActiveImage = sprite.Value.AssetReference ?? "";
                    vm.EditTextActiveFrame = sprite.Value.Frame;
                }
            }
            else if (element.IsSpriteFromTexture("ActiveImage"))
            {
                vm.EditTextActiveImage = element.GetTextureSource("ActiveImage") ?? "";
                vm.EditTextActiveFrame = 0;
            }

            // InactiveImage
            if (element.IsSpriteFromSheet("InactiveImage"))
            {
                var sprite = element.GetInactiveImageSprite();
                if (sprite.HasValue)
                {
                    vm.EditTextInactiveImage = sprite.Value.AssetReference ?? "";
                    vm.EditTextInactiveFrame = sprite.Value.Frame;
                }
            }
            else if (element.IsSpriteFromTexture("InactiveImage"))
            {
                vm.EditTextInactiveImage = element.GetTextureSource("InactiveImage") ?? "";
                vm.EditTextInactiveFrame = 0;
            }

            // MouseOverImage
            if (element.IsSpriteFromSheet("MouseOverImage"))
            {
                var sprite = element.GetSpriteSheet("MouseOverImage");
                if (sprite.HasValue)
                {
                    vm.EditTextMouseOverImage = sprite.Value.AssetReference ?? "";
                    vm.EditTextMouseOverFrame = sprite.Value.Frame;
                }
            }
            else if (element.IsSpriteFromTexture("MouseOverImage"))
            {
                vm.EditTextMouseOverImage = element.GetTextureSource("MouseOverImage") ?? "";
                vm.EditTextMouseOverFrame = 0;
            }

            // Font asset reference - load the actual font from the project
            var fontRef = element.GetFont();
            if (!string.IsNullOrEmpty(fontRef) && _assetService != null)
            {
                // Clean up any quotes that may have been incorrectly added
                vm.FontAssetReference = CleanAssetReference(fontRef);
                var fontInfo = _assetService.GetFontInfo(vm.FontAssetReference);
                if (fontInfo != null)
                {
                    vm.FontFamily = fontInfo.FontFamilyName;
                }
            }
        }

        private void LoadSliderProperties(UIElementViewModel vm, ToolkitUIElement element)
        {
            vm.SliderMinimum = element.GetSliderMinimum();
            vm.SliderMaximum = element.GetSliderMaximum();
            vm.SliderValue = element.GetSliderValue();
            vm.SliderStep = element.Get<float?>("Step") ?? 0.1f; // No toolkit getter
            vm.SliderTickFrequency = element.Get<float?>("TickFrequency") ?? 10f; // No toolkit getter
            vm.SliderTickOffset = element.GetTickOffset();
            vm.SliderOrientation = element.Get<string>("Orientation") ?? "Horizontal"; // No slider-specific getter
            vm.AreTicksDisplayed = element.GetAreTicksDisplayed();
            vm.ShouldSnapToTicks = element.GetShouldSnapToTicks();
            vm.IsDirectionReversed = element.GetIsDirectionReversed();

            // Track starting offsets - dot syntax (v1.6.0+)
            vm.TrackStartingOffsetLeft = element.Get<float?>("TrackStartingOffsets.Left") ?? 0f;
            vm.TrackStartingOffsetTop = element.Get<float?>("TrackStartingOffsets.Top") ?? 0f;
            vm.TrackStartingOffsetRight = element.Get<float?>("TrackStartingOffsets.Right") ?? 0f;
            vm.TrackStartingOffsetBottom = element.Get<float?>("TrackStartingOffsets.Bottom") ?? 0f;

            // 5 sprite images - use proper toolkit helpers
            // TrackBackgroundImage
            if (element.IsSpriteFromSheet("TrackBackgroundImage"))
            {
                var sprite = element.GetTrackBackgroundImageSprite();
                if (sprite.HasValue)
                {
                    vm.SliderTrackBackgroundImage = sprite.Value.AssetReference ?? "";
                    vm.SliderTrackBackgroundFrame = sprite.Value.Frame;
                }
            }
            else if (element.IsSpriteFromTexture("TrackBackgroundImage"))
            {
                vm.SliderTrackBackgroundImage = element.GetTextureSource("TrackBackgroundImage") ?? "";
                vm.SliderTrackBackgroundFrame = 0;
            }

            // TrackForegroundImage
            if (element.IsSpriteFromSheet("TrackForegroundImage"))
            {
                var sprite = element.GetSpriteSheet("TrackForegroundImage");
                if (sprite.HasValue)
                {
                    vm.SliderTrackForegroundImage = sprite.Value.AssetReference ?? "";
                    vm.SliderTrackForegroundFrame = sprite.Value.Frame;
                }
            }
            else if (element.IsSpriteFromTexture("TrackForegroundImage"))
            {
                vm.SliderTrackForegroundImage = element.GetTextureSource("TrackForegroundImage") ?? "";
                vm.SliderTrackForegroundFrame = 0;
            }

            // ThumbImage
            if (element.IsSpriteFromSheet("ThumbImage"))
            {
                var sprite = element.GetThumbImageSprite();
                if (sprite.HasValue)
                {
                    vm.SliderThumbImage = sprite.Value.AssetReference ?? "";
                    vm.SliderThumbFrame = sprite.Value.Frame;
                }
            }
            else if (element.IsSpriteFromTexture("ThumbImage"))
            {
                vm.SliderThumbImage = element.GetTextureSource("ThumbImage") ?? "";
                vm.SliderThumbFrame = 0;
            }

            // MouseOverThumbImage
            if (element.IsSpriteFromSheet("MouseOverThumbImage"))
            {
                var sprite = element.GetSpriteSheet("MouseOverThumbImage");
                if (sprite.HasValue)
                {
                    vm.SliderMouseOverThumbImage = sprite.Value.AssetReference ?? "";
                    vm.SliderMouseOverThumbFrame = sprite.Value.Frame;
                }
            }
            else if (element.IsSpriteFromTexture("MouseOverThumbImage"))
            {
                vm.SliderMouseOverThumbImage = element.GetTextureSource("MouseOverThumbImage") ?? "";
                vm.SliderMouseOverThumbFrame = 0;
            }

            // TickImage
            if (element.IsSpriteFromSheet("TickImage"))
            {
                var sprite = element.GetSpriteSheet("TickImage");
                if (sprite.HasValue)
                {
                    vm.SliderTickImage = sprite.Value.AssetReference ?? "";
                    vm.SliderTickFrame = sprite.Value.Frame;
                }
            }
            else if (element.IsSpriteFromTexture("TickImage"))
            {
                vm.SliderTickImage = element.GetTextureSource("TickImage") ?? "";
                vm.SliderTickFrame = 0;
            }
        }

        private void LoadToggleButtonProperties(UIElementViewModel vm, ToolkitUIElement element)
        {
            vm.ToggleState = element.Get<string>("State") ?? "UnChecked";  // No string getter, only IsChecked() bool
            vm.IsThreeState = element.Get<bool?>("IsThreeState") ?? false; // No toolkit getter
            vm.ToggleClickMode = element.GetClickMode();

            // 3-state images - use proper toolkit helpers
            // CheckedImage
            if (element.IsSpriteFromSheet("CheckedImage"))
            {
                var sprite = element.GetCheckedImageSprite();
                if (sprite.HasValue)
                {
                    vm.ToggleCheckedImage = sprite.Value.AssetReference ?? "";
                    vm.ToggleCheckedFrame = sprite.Value.Frame;
                }
            }
            else if (element.IsSpriteFromTexture("CheckedImage"))
            {
                vm.ToggleCheckedImage = element.GetTextureSource("CheckedImage") ?? "";
                vm.ToggleCheckedFrame = 0;
            }

            // UncheckedImage
            if (element.IsSpriteFromSheet("UncheckedImage"))
            {
                var sprite = element.GetUncheckedImageSprite();
                if (sprite.HasValue)
                {
                    vm.ToggleUncheckedImage = sprite.Value.AssetReference ?? "";
                    vm.ToggleUncheckedFrame = sprite.Value.Frame;
                }
            }
            else if (element.IsSpriteFromTexture("UncheckedImage"))
            {
                vm.ToggleUncheckedImage = element.GetTextureSource("UncheckedImage") ?? "";
                vm.ToggleUncheckedFrame = 0;
            }

            // IndeterminateImage
            if (element.IsSpriteFromSheet("IndeterminateImage"))
            {
                var sprite = element.GetIndeterminateImageSprite();
                if (sprite.HasValue)
                {
                    vm.ToggleIndeterminateImage = sprite.Value.AssetReference ?? "";
                    vm.ToggleIndeterminateFrame = sprite.Value.Frame;
                }
            }
            else if (element.IsSpriteFromTexture("IndeterminateImage"))
            {
                vm.ToggleIndeterminateImage = element.GetTextureSource("IndeterminateImage") ?? "";
                vm.ToggleIndeterminateFrame = 0;
            }
        }

        private void LoadModalElementProperties(UIElementViewModel vm, ToolkitUIElement element)
        {
            vm.IsModal = element.GetIsModal();

            // Overlay color - dot syntax (v1.6.0+)
            var r = element.Get<byte?>("OverlayColor.R") ?? 0;
            var g = element.Get<byte?>("OverlayColor.G") ?? 0;
            var b = element.Get<byte?>("OverlayColor.B") ?? 0;
            var a = element.Get<byte?>("OverlayColor.A") ?? 128;
            vm.OverlayColor = Color.FromArgb(a, r, g, b);
        }

        private void LoadUniformGridProperties(UIElementViewModel vm, ToolkitUIElement element)
        {
            vm.UniformGridRows = element.Get<int?>("Rows") ?? 1;
            vm.UniformGridColumns = element.Get<int?>("Columns") ?? 1;
        }

        private void LoadContentDecoratorProperties(UIElementViewModel vm, ToolkitUIElement element)
        {
            // Border color - dot syntax (v1.6.0+)
            var r = element.Get<byte?>("BorderColor.R") ?? 0;
            var g = element.Get<byte?>("BorderColor.G") ?? 0;
            var b = element.Get<byte?>("BorderColor.B") ?? 0;
            var a = element.Get<byte?>("BorderColor.A") ?? 0;
            vm.BorderColor = Color.FromArgb(a, r, g, b);

            // Border thickness - dot syntax (v1.6.0+)
            vm.BorderThicknessLeft = element.Get<float?>("BorderThickness.Left") ?? 0f;
            vm.BorderThicknessTop = element.Get<float?>("BorderThickness.Top") ?? 0f;
            vm.BorderThicknessRight = element.Get<float?>("BorderThickness.Right") ?? 0f;
            vm.BorderThicknessBottom = element.Get<float?>("BorderThickness.Bottom") ?? 0f;

            // Background image - use proper toolkit helpers
            if (element.IsSpriteFromSheet("BackgroundImage"))
            {
                var sprite = element.GetBackgroundImageSprite();
                if (sprite.HasValue)
                {
                    vm.BackgroundImageSource = sprite.Value.AssetReference ?? "";
                    vm.BackgroundImageFrame = sprite.Value.Frame;
                    vm.ContentDecoratorImageMode = "SpriteSheet";
                }
            }
            else if (element.IsSpriteFromTexture("BackgroundImage"))
            {
                vm.BackgroundImageSource = element.GetBackgroundImageTexture() ?? "";
                vm.BackgroundImageFrame = 0;
                vm.ContentDecoratorImageMode = "Texture";
            }
        }

        #endregion

        #region Save Document

        /// <summary>
        /// Save the current document (requires existing file path)
        /// </summary>
        public void SaveDocument(List<UIElementViewModel> rootElements, double designWidth, double designHeight)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
                throw new InvalidOperationException("No file path set. Use SaveDocumentAs() instead.");

            if (_currentUIPage == null)
                throw new InvalidOperationException("No UIPage loaded. Use SaveDocumentAs() for new files.");

            // Set design resolution (required for SetPosition to calculate 4-margin positioning)
            _currentUIPage.SetDesignResolution((float)designWidth, (float)designHeight, 1000f);

            // Get the existing root Grid (toolkit requires one)
            var toolkitRootGrid = _currentUIPage.RootElements.FirstOrDefault();

            // Clear all elements except root Grid (must remove from AllElements list, not just Children dict)
            if (toolkitRootGrid != null)
            {
                // Get all elements except the root grid
                var elementsToRemove = _currentUIPage.AllElements.Where(e => e.Id != toolkitRootGrid.Id).ToList();
                foreach (var element in elementsToRemove)
                {
                    _currentUIPage.RemoveElement(element);
                }
            }

            // Convert ViewModels to UIPage using existing root Grid
            ConvertViewModelsToUIPageWithRoot(rootElements, toolkitRootGrid);

            // Save via toolkit
            _currentUIPage.Save();

            HasUnsavedChanges = false;
        }

        /// <summary>
        /// Save the document to a new file path
        /// </summary>
        public string SaveDocumentAs(List<UIElementViewModel> rootElements, string filePath, double designWidth, double designHeight)
        {
            if (_connectedProject == null)
                throw new InvalidOperationException("No project connected. Use SetProject() first.");

            // Always create a fresh UIPage for SaveAs to avoid any state issues
            // Extract just the filename without extension for the page name
            var pageName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            var relativePath = System.IO.Path.GetDirectoryName(filePath)?.Replace(_connectedProject.AssetsPath, "").TrimStart('\\', '/') ?? "UI";

            _currentUIPage = _connectedProject.CreateUIPage(pageName, relativePath);

            // Set design resolution FIRST (required for SetPosition to calculate 4-margin positioning)
            _currentUIPage.SetDesignResolution((float)designWidth, (float)designHeight, 1000f);

            // IMPORTANT: Toolkit auto-creates a root Grid - we use it, don't delete it!
            // Get the toolkit's root Grid to use as parent for our elements
            var toolkitRootGrid = _currentUIPage.RootElements.FirstOrDefault();

            // Convert ViewModels to UIPage using toolkit's root Grid as parent
            ConvertViewModelsToUIPageWithRoot(rootElements, toolkitRootGrid);

            // Save to the specified path
            _currentUIPage.SaveAs(filePath);
            _currentFilePath = filePath;

            HasUnsavedChanges = false;

            return filePath;
        }

        /// <summary>
        /// Clear all elements from the current UIPage
        /// </summary>
        private void ClearAllElements()
        {
            if (_currentUIPage == null) return;

            // Remove all existing elements to prevent duplicates
            // Must iterate over a copy since we're modifying the collection
            var existingElements = _currentUIPage.AllElements.ToList();
            foreach (var element in existingElements)
            {
                try
                {
                    _currentUIPage.RemoveElement(element);
                }
                catch
                {
                    // Element may already be removed as child of another - ignore
                }
            }
        }

        /// <summary>
        /// Convert ViewModels to toolkit UIPage using the toolkit's existing root Grid
        /// </summary>
        private void ConvertViewModelsToUIPageWithRoot(List<UIElementViewModel> rootElements, ToolkitUIElement? toolkitRootGrid)
        {
            if (_connectedProject == null || _currentUIPage == null)
                throw new InvalidOperationException("Project or UIPage not initialized");

            // Our editor has a hidden RootGrid (IsSystemElement) that maps to toolkit's auto-created root Grid
            // We DON'T create our RootGrid - we use toolkit's. Just add children to it.
            foreach (var rootVM in rootElements)
            {
                if (rootVM.IsSystemElement)
                {
                    // This is our hidden RootGrid - skip creating it, just add its children to toolkit's root
                    // Pass sibling index for Canvas.ZIndex to preserve draw order
                    int siblingIndex = 0;
                    foreach (var childVM in rootVM.Children)
                    {
                        ConvertViewModelToToolkitElement(childVM, _currentUIPage, toolkitRootGrid, siblingIndex++);
                    }
                }
                else
                {
                    // Regular element at root level - add to toolkit's root Grid
                    ConvertViewModelToToolkitElement(rootVM, _currentUIPage, toolkitRootGrid, 0);
                }
            }
        }

        /// <summary>
        /// Convert ViewModels to toolkit UIPage (legacy - creates own root)
        /// </summary>
        private void ConvertViewModelsToUIPage(List<UIElementViewModel> rootElements, bool skipClear = false)
        {
            if (_connectedProject == null || _currentUIPage == null)
                throw new InvalidOperationException("Project or UIPage not initialized");

            // Clear existing elements first to prevent duplicates on re-save
            // (skip if caller already cleared)
            if (!skipClear)
            {
                ClearAllElements();
            }

            // Convert our ViewModels to toolkit elements
            int siblingIndex = 0;
            foreach (var rootVM in rootElements)
            {
                ConvertViewModelToToolkitElement(rootVM, _currentUIPage, null, siblingIndex++);
            }
        }

        /// <summary>
        /// Recursively convert a ViewModel to toolkit UIElement
        /// </summary>
        /// <param name="vm">The ViewModel to convert</param>
        /// <param name="page">The UIPage to add the element to</param>
        /// <param name="parent">The parent toolkit element (null for root elements)</param>
        /// <param name="siblingIndex">The index among siblings, used to set Canvas.ZIndex for draw order</param>
        private void ConvertViewModelToToolkitElement(UIElementViewModel vm, UIPage page, ToolkitUIElement? parent, int siblingIndex = 0)
        {
            // NOTE: We DO export system elements (hidden root Grid) - they're the actual root container
            // IsSystemElement is only for hiding from editor UI, not for skipping export

            // Create element via toolkit
            var element = page.CreateElement(vm.ElementType, vm.Name, parent);

            // Use toolkit helper methods for layout (v1.6.0+)
            // SetPosition properly calculates margins for absolute positioning
            element.SetPosition((float)vm.X, (float)vm.Y);
            element.SetSize((float)vm.Width, (float)vm.Height);
            element.SetAlignment("Left", "Top"); // Canvas positioning requires Left/Top alignment

            // Common appearance properties (v1.6.0+)
            element.SetOpacity((float)vm.Opacity);

            // Set Panel.ZIndex using toolkit helper - controls draw order in Stride
            element.SetZIndex(vm.ZIndex);

            // Appearance properties - use toolkit helpers
            if (vm.DrawLayerNumber != 0)
                element.SetDrawLayer(vm.DrawLayerNumber);
            if (vm.ClipToBounds)
                element.SetClipToBounds(vm.ClipToBounds);

            // Behavior properties - use toolkit helpers
            if (vm.Visibility != "Visible")
                element.SetVisibility(false);
            if (!vm.IsEnabled)
                element.SetIsEnabled(false);
            if (!vm.CanBeHitByUser)
                element.SetCanBeHitByUser(false);

            // Background color using helper
            if (vm.BackgroundColor.A > 0)
            {
                element.SetBackgroundColor(
                    vm.BackgroundColor.R,
                    vm.BackgroundColor.G,
                    vm.BackgroundColor.B,
                    vm.BackgroundColor.A);
            }

            // Type-specific properties
            switch (vm.ElementType)
            {
                case "TextBlock":
                    SaveTextBlockProperties(element, vm);
                    break;

                case "Button":
                    SaveButtonProperties(element, vm, page);
                    break;

                case "ImageElement":
                    SaveImageElementProperties(element, vm);
                    break;

                case "StackPanel":
                    SaveStackPanelProperties(element, vm);
                    break;

                case "ScrollViewer":
                    SaveScrollViewerProperties(element, vm);
                    break;

                case "EditText":
                    SaveEditTextProperties(element, vm);
                    break;

                case "Slider":
                    SaveSliderProperties(element, vm);
                    break;

                case "ToggleButton":
                    SaveToggleButtonProperties(element, vm, page);
                    break;

                case "ModalElement":
                    SaveModalElementProperties(element, vm);
                    break;

                case "UniformGrid":
                    SaveUniformGridProperties(element, vm);
                    break;

                case "ContentDecorator":
                case "Border":
                    SaveContentDecoratorProperties(element, vm);
                    break;
            }

            // Recursively convert children with sibling indices for Canvas.ZIndex
            int childIndex = 0;
            foreach (var childVM in vm.Children)
            {
                ConvertViewModelToToolkitElement(childVM, page, element, childIndex++);
            }
        }

        private void SaveTextBlockProperties(ToolkitUIElement element, UIElementViewModel vm)
        {
            element.SetText(vm.Text);
            element.SetFontSize((float)vm.FontSize);
            element.Set("TextAlignment", vm.TextAlignment); // No toolkit helper
            element.SetWrapText(vm.WrapText);
            if (vm.DoNotSnapText)
                element.SetDoNotSnapText(true);

            // Use helper method
            element.SetTextColor(vm.TextColor.R, vm.TextColor.G, vm.TextColor.B, vm.TextColor.A);

            if (vm.TextOutlineThickness > 0)
            {
                element.SetOutlineColor(vm.TextOutlineColor.R, vm.TextOutlineColor.G, vm.TextOutlineColor.B, vm.TextOutlineColor.A);
                element.SetOutlineThickness((float)vm.TextOutlineThickness);
            }

            // Font asset reference (format: "guid:path")
            if (!string.IsNullOrEmpty(vm.FontAssetReference))
            {
                element.Set("Font", vm.FontAssetReference); // SetFont takes AssetReference, not string
            }
        }

        /// <summary>
        /// Get an AssetReference from a "guid:path" format string
        /// </summary>
        private AssetReference? GetAssetFromReference(string reference)
        {
            if (string.IsNullOrEmpty(reference) || _connectedProject == null)
                return null;

            // Format is "guid:path" - extract the guid
            var colonIndex = reference.IndexOf(':');
            if (colonIndex > 0)
            {
                var guid = reference.Substring(0, colonIndex);
                return _connectedProject.FindAssetByGuid(guid);
            }

            // Fallback: try to find by path
            return _connectedProject.FindAssetByPath(reference);
        }

        private void SaveButtonProperties(ToolkitUIElement element, UIElementViewModel vm, UIPage page)
        {
            // Button needs a TextBlock child for the label (Content property)
            var textBlock = page.CreateElement("TextBlock", vm.Name + "_Text", element);
            textBlock.Set("Text", vm.ButtonText);
            element.Set("Content", textBlock); // Reference to TextBlock

            // Click mode
            if (vm.ClickMode != "Release")
                element.SetClickMode(vm.ClickMode);

            // 3-state images using helpers (v1.6.0+)
            // If AssetReference is null but string has value, look it up from project
            bool useSpriteSheet = vm.ButtonImageMode == "SpriteSheet";

            var notPressedRef = vm.ButtonNotPressedImageAsset as AssetReference;
            if (notPressedRef == null && !string.IsNullOrEmpty(vm.ButtonNotPressedImage))
                notPressedRef = GetAssetFromReference(vm.ButtonNotPressedImage);
            if (notPressedRef != null)
            {
                if (useSpriteSheet)
                    element.SetNotPressedImage(notPressedRef, vm.ButtonNotPressedFrame);
                else
                    element.SetNotPressedTexture(notPressedRef);
            }

            var pressedRef = vm.ButtonPressedImageAsset as AssetReference;
            if (pressedRef == null && !string.IsNullOrEmpty(vm.ButtonPressedImage))
                pressedRef = GetAssetFromReference(vm.ButtonPressedImage);
            if (pressedRef != null)
            {
                if (useSpriteSheet)
                    element.SetPressedImage(pressedRef, vm.ButtonPressedFrame);
                else
                    element.SetPressedTexture(pressedRef);
            }

            var mouseOverRef = vm.ButtonMouseOverImageAsset as AssetReference;
            if (mouseOverRef == null && !string.IsNullOrEmpty(vm.ButtonMouseOverImage))
                mouseOverRef = GetAssetFromReference(vm.ButtonMouseOverImage);
            if (mouseOverRef != null)
            {
                if (useSpriteSheet)
                    element.SetMouseOverImage(mouseOverRef, vm.ButtonMouseOverFrame);
                else
                    element.SetMouseOverTexture(mouseOverRef);
            }
        }

        private void SaveImageElementProperties(ToolkitUIElement element, UIElementViewModel vm)
        {
            // Set image source using helper methods (v1.6.0+)
            // ImageAssetReference is stored as object? but needs to be cast to AssetReference
            // If ImageAssetReference is null but ImageSource has a value, look it up from the project
            var assetRef = vm.ImageAssetReference as AssetReference;
            if (assetRef == null && !string.IsNullOrEmpty(vm.ImageSource) && vm.ImageSource != "(None)")
            {
                assetRef = GetAssetFromReference(vm.ImageSource);
            }

            if (assetRef != null)
            {
                if (vm.ImageAssetType == "SpriteSheet")
                {
                    // Use SetSprite for sprite sheets (handles sheet + frame)
                    element.SetSprite(assetRef, vm.SpriteFrame);
                }
                else
                {
                    // Use SetTexture for single textures
                    element.SetTexture(assetRef);
                }
            }

            // Tint color using helper (v1.6.0+)
            element.SetColor(vm.ImageTintColor.R, vm.ImageTintColor.G, vm.ImageTintColor.B, vm.ImageTintColor.A);

            // Stretch properties using helpers (v1.6.0+)
            element.SetStretchType(vm.StretchType);
            element.SetStretchDirection(vm.StretchDirection);
        }

        private void SaveStackPanelProperties(ToolkitUIElement element, UIElementViewModel vm)
        {
            element.SetOrientation(vm.StackPanelOrientation);
            if (vm.ItemVirtualizationEnabled)
                element.Set("ItemVirtualizationEnabled", true); // No toolkit helper
        }

        private void SaveScrollViewerProperties(ToolkitUIElement element, UIElementViewModel vm)
        {
            // ScrollMode - Stride default is Horizontal
            if (vm.ScrollMode != "Horizontal")
                element.SetScrollMode(vm.ScrollMode);

            // ScrollBar properties - use toolkit helpers
            if (Math.Abs(vm.ScrollBarThickness - 6.0) > 0.1)
                element.SetScrollBarThickness((float)vm.ScrollBarThickness);
            if (Math.Abs(vm.ScrollingSpeed - 800.0) > 1.0)
                element.SetScrollingSpeed((float)vm.ScrollingSpeed);
            if (Math.Abs(vm.Deceleration - 1500.0) > 1.0)
                element.SetDeceleration((float)vm.Deceleration);
            if (Math.Abs(vm.ScrollStartThreshold - 10.0) > 0.1)
                element.SetScrollStartThreshold((float)vm.ScrollStartThreshold);
            if (!vm.TouchScrollingEnabled)
                element.SetTouchScrollingEnabled(false);
            if (vm.SnapToAnchors)  // Default is false
                element.SetSnapToAnchors(true);

            // ScrollBarColor - use toolkit helper
            element.SetScrollBarColor(vm.ScrollBarColor.R, vm.ScrollBarColor.G, vm.ScrollBarColor.B, vm.ScrollBarColor.A);
        }

        private void SaveEditTextProperties(ToolkitUIElement element, UIElementViewModel vm)
        {
            element.SetText(vm.Text);
            element.SetFontSize((float)vm.FontSize);

            // EditText uses TextColor but SetTextColor helper only works for TextBlock/ScrollingText
            // Use raw Set with dictionary for EditText
            element.Set("TextColor", new Dictionary<string, object>
            {
                ["R"] = (int)vm.TextColor.R,
                ["G"] = (int)vm.TextColor.G,
                ["B"] = (int)vm.TextColor.B,
                ["A"] = (int)vm.TextColor.A
            });

            element.SetMaxLength(vm.MaxLength);
            element.SetIsReadOnly(vm.IsReadOnly);

            // EditText properties - use toolkit helpers
            if (vm.InputType != "None")
                element.SetInputType(vm.InputType);
            if (vm.MinLines > 1)
                element.SetMinLines(vm.MinLines);
            if (vm.MaxLines < int.MaxValue)
                element.SetMaxLines(vm.MaxLines);
            if (Math.Abs(vm.CaretWidth - 1.0) > 0.01)
                element.Set("CaretWidth", (float)vm.CaretWidth); // No toolkit helper
            if (Math.Abs(vm.CaretFrequency - 1.0) > 0.01)
                element.SetCaretFrequency((float)vm.CaretFrequency);

            // Colors using helper methods (v1.6.0+)
            element.SetCaretColor(vm.CaretColor.R, vm.CaretColor.G, vm.CaretColor.B, vm.CaretColor.A);
            element.SetSelectionColor(vm.SelectionColor.R, vm.SelectionColor.G, vm.SelectionColor.B, vm.SelectionColor.A);
            element.Set("IMESelectionColor", new Dictionary<string, object>
            {
                ["R"] = (int)vm.IMESelectionColor.R,
                ["G"] = (int)vm.IMESelectionColor.G,
                ["B"] = (int)vm.IMESelectionColor.B,
                ["A"] = (int)vm.IMESelectionColor.A
            });

            // 3-state images using proper helper methods (v1.6.0+)
            // If AssetReference is null but string has value, look it up from project
            bool useSpriteSheet = vm.EditTextImageMode == "SpriteSheet";

            var activeRef = vm.EditTextActiveImageAsset as AssetReference;
            if (activeRef == null && !string.IsNullOrEmpty(vm.EditTextActiveImage))
                activeRef = GetAssetFromReference(vm.EditTextActiveImage);
            if (activeRef != null)
            {
                if (useSpriteSheet)
                    element.SetActiveImage(activeRef, vm.EditTextActiveFrame);
                else
                    element.SetActiveTexture(activeRef);
            }

            var inactiveRef = vm.EditTextInactiveImageAsset as AssetReference;
            if (inactiveRef == null && !string.IsNullOrEmpty(vm.EditTextInactiveImage))
                inactiveRef = GetAssetFromReference(vm.EditTextInactiveImage);
            if (inactiveRef != null)
            {
                if (useSpriteSheet)
                    element.SetInactiveImage(inactiveRef, vm.EditTextInactiveFrame);
                else
                    element.SetInactiveTexture(inactiveRef);
            }

            var mouseOverRef = vm.EditTextMouseOverImageAsset as AssetReference;
            if (mouseOverRef == null && !string.IsNullOrEmpty(vm.EditTextMouseOverImage))
                mouseOverRef = GetAssetFromReference(vm.EditTextMouseOverImage);
            if (mouseOverRef != null)
            {
                if (useSpriteSheet)
                    element.SetEditTextMouseOverImage(mouseOverRef, vm.EditTextMouseOverFrame);
                else
                    element.SetEditTextMouseOverTexture(mouseOverRef);
            }

            // Font asset reference (format: "guid:path")
            if (!string.IsNullOrEmpty(vm.FontAssetReference))
            {
                element.Set("Font", vm.FontAssetReference);
            }
        }

        private void SaveSliderProperties(ToolkitUIElement element, UIElementViewModel vm)
        {
            // Use helper methods (v1.6.0+)
            element.SetRange((float)vm.SliderMinimum, (float)vm.SliderMaximum);
            element.SetValue((float)vm.SliderValue);
            element.SetStep((float)vm.SliderStep);

            // Tick properties - use toolkit helpers where available
            if (vm.SliderTickFrequency > 0)
                element.Set("TickFrequency", (float)vm.SliderTickFrequency); // No toolkit helper
            if (vm.SliderTickOffset > 0)
                element.SetTickOffset((float)vm.SliderTickOffset);
            if (vm.SliderOrientation != "Horizontal")
                element.Set("Orientation", vm.SliderOrientation); // No slider-specific helper
            if (vm.AreTicksDisplayed)
                element.SetAreTicksDisplayed(true);
            if (vm.ShouldSnapToTicks)
                element.SetShouldSnapToTicks(true);
            if (vm.IsDirectionReversed)
                element.SetIsDirectionReversed(true);

            // Track starting offsets - use Thickness format
            if (vm.TrackStartingOffsetLeft > 0 || vm.TrackStartingOffsetTop > 0 ||
                vm.TrackStartingOffsetRight > 0 || vm.TrackStartingOffsetBottom > 0)
            {
                element.Set("TrackStartingOffsets", new Dictionary<string, object>
                {
                    ["Left"] = (float)vm.TrackStartingOffsetLeft,
                    ["Top"] = (float)vm.TrackStartingOffsetTop,
                    ["Right"] = (float)vm.TrackStartingOffsetRight,
                    ["Bottom"] = (float)vm.TrackStartingOffsetBottom
                });
            }

            // 5 sprite images using proper helper methods (v1.6.0+)
            // If AssetReference is null but string has value, look it up from project
            bool useSpriteSheet = vm.SliderImageMode == "SpriteSheet";

            var trackBgRef = vm.SliderTrackBackgroundImageAsset as AssetReference;
            if (trackBgRef == null && !string.IsNullOrEmpty(vm.SliderTrackBackgroundImage))
                trackBgRef = GetAssetFromReference(vm.SliderTrackBackgroundImage);
            if (trackBgRef != null)
            {
                if (useSpriteSheet)
                    element.SetTrackBackgroundImage(trackBgRef, vm.SliderTrackBackgroundFrame);
                else
                    element.SetTrackBackgroundTexture(trackBgRef);
            }

            var trackFgRef = vm.SliderTrackForegroundImageAsset as AssetReference;
            if (trackFgRef == null && !string.IsNullOrEmpty(vm.SliderTrackForegroundImage))
                trackFgRef = GetAssetFromReference(vm.SliderTrackForegroundImage);
            if (trackFgRef != null)
            {
                if (useSpriteSheet)
                    element.SetTrackForegroundImage(trackFgRef, vm.SliderTrackForegroundFrame);
                else
                    element.SetTrackForegroundTexture(trackFgRef);
            }

            var thumbRef = vm.SliderThumbImageAsset as AssetReference;
            if (thumbRef == null && !string.IsNullOrEmpty(vm.SliderThumbImage))
                thumbRef = GetAssetFromReference(vm.SliderThumbImage);
            if (thumbRef != null)
            {
                if (useSpriteSheet)
                    element.SetThumbImage(thumbRef, vm.SliderThumbFrame);
                else
                    element.SetThumbTexture(thumbRef);
            }

            var mouseOverThumbRef = vm.SliderMouseOverThumbImageAsset as AssetReference;
            if (mouseOverThumbRef == null && !string.IsNullOrEmpty(vm.SliderMouseOverThumbImage))
                mouseOverThumbRef = GetAssetFromReference(vm.SliderMouseOverThumbImage);
            if (mouseOverThumbRef != null)
            {
                if (useSpriteSheet)
                    element.SetMouseOverThumbImage(mouseOverThumbRef, vm.SliderMouseOverThumbFrame);
                else
                    element.SetMouseOverThumbTexture(mouseOverThumbRef);
            }

            var tickRef = vm.SliderTickImageAsset as AssetReference;
            if (tickRef == null && !string.IsNullOrEmpty(vm.SliderTickImage))
                tickRef = GetAssetFromReference(vm.SliderTickImage);
            if (tickRef != null)
            {
                if (useSpriteSheet)
                    element.SetTickImage(tickRef, vm.SliderTickFrame);
                else
                {
                    // No texture helper for TickImage, use raw Set
                    element.Set("TickImage", new Dictionary<string, object>
                    {
                        ["!SpriteFromTexture"] = "",
                        ["Texture"] = tickRef.Reference
                    });
                }
            }
        }

        private void SaveToggleButtonProperties(ToolkitUIElement element, UIElementViewModel vm, UIPage page)
        {
            // State - only set if not default (UnChecked)
            if (vm.ToggleState != "UnChecked")
                element.Set("State", vm.ToggleState);

            if (vm.IsThreeState)
                element.Set("IsThreeState", true);

            // ClickMode - inherited from ButtonBase
            if (vm.ToggleClickMode != "Release")
                element.SetClickMode(vm.ToggleClickMode);

            // 3-state images using proper helper methods (v1.6.0+)
            // If AssetReference is null but string has value, look it up from project
            bool useSpriteSheet = vm.ToggleImageMode == "SpriteSheet";

            var checkedRef = vm.ToggleCheckedImageAsset as AssetReference;
            if (checkedRef == null && !string.IsNullOrEmpty(vm.ToggleCheckedImage))
                checkedRef = GetAssetFromReference(vm.ToggleCheckedImage);
            if (checkedRef != null)
            {
                if (useSpriteSheet)
                    element.SetCheckedImage(checkedRef, vm.ToggleCheckedFrame);
                else
                    element.SetCheckedTexture(checkedRef);
            }

            var uncheckedRef = vm.ToggleUncheckedImageAsset as AssetReference;
            if (uncheckedRef == null && !string.IsNullOrEmpty(vm.ToggleUncheckedImage))
                uncheckedRef = GetAssetFromReference(vm.ToggleUncheckedImage);
            if (uncheckedRef != null)
            {
                if (useSpriteSheet)
                    element.SetUncheckedImage(uncheckedRef, vm.ToggleUncheckedFrame);
                else
                    element.SetUncheckedTexture(uncheckedRef);
            }

            var indeterminateRef = vm.ToggleIndeterminateImageAsset as AssetReference;
            if (indeterminateRef == null && !string.IsNullOrEmpty(vm.ToggleIndeterminateImage))
                indeterminateRef = GetAssetFromReference(vm.ToggleIndeterminateImage);
            if (indeterminateRef != null && vm.IsThreeState)
            {
                if (useSpriteSheet)
                    element.SetIndeterminateImage(indeterminateRef, vm.ToggleIndeterminateFrame);
                else
                    element.SetIndeterminateTexture(indeterminateRef);
            }
        }

        private void SaveModalElementProperties(ToolkitUIElement element, UIElementViewModel vm)
        {
            if (!vm.IsModal)
                element.SetIsModal(false);

            // Overlay color - use toolkit helper
            if (vm.OverlayColor.A > 0)
            {
                element.SetOverlayColor(vm.OverlayColor.R, vm.OverlayColor.G, vm.OverlayColor.B, vm.OverlayColor.A);
            }
        }

        private void SaveUniformGridProperties(ToolkitUIElement element, UIElementViewModel vm)
        {
            if (vm.UniformGridRows > 1)
                element.Set("Rows", vm.UniformGridRows);
            if (vm.UniformGridColumns > 1)
                element.Set("Columns", vm.UniformGridColumns);
        }

        private void SaveContentDecoratorProperties(ToolkitUIElement element, UIElementViewModel vm)
        {
            // Border color - use toolkit helper
            if (vm.BorderColor.A > 0)
            {
                element.SetBorderColor(vm.BorderColor.R, vm.BorderColor.G, vm.BorderColor.B, vm.BorderColor.A);
            }

            // Border thickness - use toolkit helper
            if (vm.BorderThicknessLeft > 0 || vm.BorderThicknessTop > 0 ||
                vm.BorderThicknessRight > 0 || vm.BorderThicknessBottom > 0)
            {
                element.SetBorderThickness(
                    (float)vm.BorderThicknessLeft,
                    (float)vm.BorderThicknessTop,
                    (float)vm.BorderThicknessRight,
                    (float)vm.BorderThicknessBottom);
            }

            // Background image using sprite/texture helpers
            // If AssetReference is null but string has value, look it up from project
            bool useSpriteSheet = vm.ContentDecoratorImageMode == "SpriteSheet";
            var bgRef = vm.BackgroundImageAsset as AssetReference;
            if (bgRef == null && !string.IsNullOrEmpty(vm.BackgroundImageSource))
                bgRef = GetAssetFromReference(vm.BackgroundImageSource);
            if (bgRef != null)
            {
                if (useSpriteSheet)
                {
                    // Use raw Set for background image sprite
                    element.Set("BackgroundImage", new Dictionary<string, object>
                    {
                        ["Sheet"] = bgRef.Reference,
                        ["CurrentFrame"] = vm.BackgroundImageFrame
                    });
                }
                else
                {
                    // Use raw Set for background image texture
                    element.Set("BackgroundImage", new Dictionary<string, object>
                    {
                        ["!SpriteFromTexture"] = "",
                        ["Texture"] = bgRef.Reference
                    });
                }
            }
        }

        #endregion

        #region Text Measurement

        /// <summary>
        /// Measure text size using WPF's FormattedText - similar to Stride's Font.MeasureString()
        /// </summary>
        private (double Width, double Height) MeasureText(string text, double fontSize)
        {
            if (string.IsNullOrEmpty(text))
                return (0, fontSize * 1.2); // Empty text still has height

            try
            {
                // Use 96 DPI as standard (WPF default)
                // Stride's default font is Arial Bold
                var formattedText = new FormattedText(
                    text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new System.Windows.Media.FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    fontSize,
                    Brushes.Black,
                    96.0 / 96.0); // pixelsPerDip = 1.0 for 96 DPI

                return (formattedText.Width, formattedText.Height);
            }
            catch
            {
                // Fallback: estimate based on character count
                // Average character width is roughly 0.5-0.6 of font size for most fonts
                return (text.Length * fontSize * 0.55, fontSize * 1.2);
            }
        }

        #endregion
    }
}

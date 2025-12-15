// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using HS.Stride.UI.Editor.Core.Abstractions;
using HS.Stride.UI.Editor.Models;

namespace HS.Stride.UI.Editor.Core.Services.Interfaces
{
    /// <summary>
    /// Service for managing Stride project assets
    /// </summary>
    public interface IAssetService
    {
        /// <summary>
        /// Whether a project is currently connected
        /// </summary>
        bool IsProjectConnected { get; }

        /// <summary>
        /// Current project path
        /// </summary>
        string? ProjectPath { get; }

        /// <summary>
        /// Connect to a Stride project
        /// </summary>
        void ConnectToProject(string projectPath);

        /// <summary>
        /// Disconnect from the current project
        /// </summary>
        void DisconnectProject();

        /// <summary>
        /// Get all project assets (textures and sprite sheets) with thumbnails
        /// </summary>
        System.Collections.ObjectModel.ObservableCollection<AssetItem> GetProjectAssets(bool forceRefresh = false);

        /// <summary>
        /// Get project assets as a list (for property panel pickers)
        /// </summary>
        List<AssetItem> GetProjectAssetsList();

        /// <summary>
        /// Load an image from an asset name
        /// </summary>
        IImageSource? LoadAssetImage(string assetName);

        /// <summary>
        /// Get a specific sprite frame from a sprite sheet by frame index
        /// Returns the cropped image for just that frame
        /// </summary>
        IImageSource? GetSpriteFrame(string assetReference, int frameIndex);

        /// <summary>
        /// Get the dimensions of an image asset, scaled to fit within maxSize while maintaining aspect ratio
        /// </summary>
        /// <param name="assetName">The asset name or reference</param>
        /// <param name="maxSize">Maximum size for the longest dimension (default 200)</param>
        /// <returns>Width and height scaled to fit, or null if image can't be loaded</returns>
        (double Width, double Height)? GetScaledImageDimensions(string assetName, double maxSize = 200);

        /// <summary>
        /// Apply a tint color to an image by multiplying pixel colors (like Stride's Color property)
        /// </summary>
        System.Windows.Media.ImageSource? ApplyTintToImage(System.Windows.Media.ImageSource source, System.Windows.Media.Color tintColor);

        /// <summary>
        /// Get all fonts in the project
        /// </summary>
        List<FontInfo> GetProjectFonts();

        /// <summary>
        /// Load a font from a font asset reference (guid:path format)
        /// Returns the WPF FontFamily to use for rendering
        /// </summary>
        System.Windows.Media.FontFamily? LoadFont(string fontAssetReference);

        /// <summary>
        /// Get font info from a font asset reference
        /// </summary>
        FontInfo? GetFontInfo(string fontAssetReference);
    }

    /// <summary>
    /// Information about a Stride font asset
    /// </summary>
    public class FontInfo
    {
        /// <summary>
        /// The asset reference (guid:path format)
        /// </summary>
        public string AssetReference { get; set; } = "";

        /// <summary>
        /// Display name for the font
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The font family name (system font or loaded from file)
        /// </summary>
        public string FontFamilyName { get; set; } = "";

        /// <summary>
        /// Whether this is a file-based font (vs system font)
        /// </summary>
        public bool IsFileBased { get; set; }

        /// <summary>
        /// Path to the .ttf/.otf file if file-based
        /// </summary>
        public string? FontFilePath { get; set; }

        /// <summary>
        /// The WPF FontFamily for rendering
        /// </summary>
        public System.Windows.Media.FontFamily? FontFamily { get; set; }
    }
}

// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HS.Stride.UI.Editor.Core.Abstractions;
using HS.Stride.UI.Editor.Core.Services.Interfaces;
using HS.Stride.UI.Editor.Models;
using HS.Stride.Editor.Toolkit.Core;
using HS.Stride.Editor.Toolkit.Core.AssetEditing;
using System.IO;

namespace HS.Stride.UI.Editor.Core.Services
{
    /// <summary>
    /// Manages Stride project assets: loading, caching, and providing access to textures and sprites
    /// </summary>
    public class AssetService : IAssetService
    {
        private StrideProject? _connectedProject;
        private ObservableCollection<AssetItem>? _cachedAssets;
        private Dictionary<string, FontInfo>? _cachedFonts;

        public bool IsProjectConnected => _connectedProject != null;
        public string? ProjectPath => _connectedProject?.ProjectPath;

        #region Project Connection

        /// <summary>
        /// Connect to a Stride project
        /// </summary>
        public void ConnectToProject(string projectPath)
        {
            _connectedProject = new StrideProject(projectPath);
            _cachedAssets = null; // Clear cache when project changes
        }

        /// <summary>
        /// Disconnect from the current project
        /// </summary>
        public void DisconnectProject()
        {
            _connectedProject = null;
            _cachedAssets = null;
            _cachedFonts = null;
        }

        /// <summary>
        /// Refresh the project by reconnecting (forces toolkit to rescan assets)
        /// </summary>
        public void RefreshProject()
        {
            if (_connectedProject == null) return;

            var projectPath = _connectedProject.ProjectPath;
            _connectedProject = new StrideProject(projectPath);
            _cachedAssets = null;
            _cachedFonts = null;
        }

        /// <summary>
        /// Get the connected project (for advanced operations)
        /// </summary>
        public StrideProject? GetConnectedProject()
        {
            return _connectedProject;
        }

        #endregion

        #region Asset Loading

        /// <summary>
        /// Get all project assets (textures and sprite sheets) with thumbnails
        /// </summary>
        public ObservableCollection<AssetItem> GetProjectAssets(bool forceRefresh = false)
        {
            if (_connectedProject == null)
                return new ObservableCollection<AssetItem>();

            // Return cached assets if available
            if (_cachedAssets != null && !forceRefresh)
                return _cachedAssets;

            var assets = new ObservableCollection<AssetItem>();

            try
            {
                // Get textures
                var textures = _connectedProject.GetTextures();
                foreach (var texture in textures)
                {
                    var imageSource = LoadAssetImage(texture.Name);
                    assets.Add(new AssetItem
                    {
                        Name = texture.Name,
                        Path = texture.Path, // Relative path from Assets folder
                        Type = "Texture",
                        Icon = "🖼️",
                        AssetReference = texture,
                        Thumbnail = imageSource?.NativeSource as ImageSource
                    });
                }

                // Get sprite sheets
                var spriteSheets = _connectedProject.GetSpriteSheets();
                foreach (var sprite in spriteSheets)
                {
                    var imageSource = LoadAssetImage(sprite.Name);
                    assets.Add(new AssetItem
                    {
                        Name = sprite.Name,
                        Path = sprite.Path, // Relative path from Assets folder
                        Type = "SpriteSheet",
                        Icon = "🎨",
                        AssetReference = sprite,
                        Thumbnail = imageSource?.NativeSource as ImageSource
                    });
                }

                // Get fonts
                var fonts = _connectedProject.GetAssets(AssetType.SpriteFont);
                foreach (var font in fonts)
                {
                    assets.Add(new AssetItem
                    {
                        Name = font.Name,
                        Path = font.Path, // Relative path from Assets folder
                        Type = "SpriteFont",
                        Icon = "🔤",
                        AssetReference = font
                    });
                }
            }
            catch
            {
                // Silently fail if assets can't be loaded
            }

            _cachedAssets = assets;
            return assets;
        }

        /// <summary>
        /// Get project assets as a list (for pickers/dialogs)
        /// </summary>
        public List<AssetItem> GetProjectAssetsList()
        {
            return GetProjectAssets(false).ToList();
        }

        #endregion

        #region Image Loading

        /// <summary>
        /// Load an asset image from the project using toolkit to find the actual source file
        /// Handles both Textures (.sdtex) and SpriteSheets (.sdsheet)
        /// </summary>
        public IImageSource? LoadAssetImage(string assetName)
        {
            if (_connectedProject == null || string.IsNullOrEmpty(assetName))
                return null;

            try
            {
                // Handle asset reference format: "guid:path" - extract just the name
                var cleanName = assetName;
                if (assetName.Contains(':'))
                {
                    cleanName = assetName.Split(':').Last();
                    // Get just the asset name from path like "UI/Textures/prototype_boxes"
                    cleanName = Path.GetFileNameWithoutExtension(cleanName);
                }

                // Find the asset by name
                var asset = _connectedProject.FindAsset(cleanName);
                if (asset == null)
                {
                    // Try finding by GUID if the name contains one
                    if (assetName.Contains(':'))
                    {
                        var guid = assetName.Split(':').First();
                        asset = _connectedProject.FindAssetByGuid(guid);
                    }
                }

                if (asset == null) return null;

                string? imagePath = null;

                // Get the actual source file path based on asset type
                if (asset.Type == AssetType.Texture)
                {
                    imagePath = GetTextureSourcePath(asset);
                }
                else if (asset.Type == AssetType.SpriteSheet)
                {
                    imagePath = GetSpriteSheetSourcePath(asset);
                }

                if (imagePath != null && File.Exists(imagePath))
                {
                    var wpfImage = LoadBitmapImage(imagePath);
                    return new WpfImageSource(wpfImage);
                }
            }
            catch
            {
                // Failed to load image, return null
            }

            return null;
        }

        /// <summary>
        /// Get the source image path for a texture asset
        /// </summary>
        private string? GetTextureSourcePath(AssetReference asset)
        {
            try
            {
                // Load the .sdtex file to get the Source property
                var textureAsset = _connectedProject!.LoadTexture(asset.Name);
                var sourcePath = textureAsset.GetSource();

                if (string.IsNullOrEmpty(sourcePath))
                    return null;

                // Source is relative to the .sdtex file location
                var assetDir = Path.GetDirectoryName(asset.FilePath);
                if (assetDir == null) return null;

                return Path.GetFullPath(Path.Combine(assetDir, sourcePath));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get the source image path for a sprite sheet asset (uses first sprite's source)
        /// </summary>
        private string? GetSpriteSheetSourcePath(AssetReference asset)
        {
            try
            {
                // Load the .sdsheet file and get the source from the first sprite
                var spriteSheetAsset = _connectedProject!.LoadSpriteSheet(asset.Name);
                var allProps = spriteSheetAsset.GetAllProperties();

                // Sprites are stored under "Sprites" key as a dictionary
                if (allProps.TryGetValue("Sprites", out var spritesObj) && spritesObj is Dictionary<string, object> sprites)
                {
                    // Get the first sprite's source
                    foreach (var spriteEntry in sprites.Values)
                    {
                        if (spriteEntry is Dictionary<string, object> spriteData &&
                            spriteData.TryGetValue("Source", out var sourceObj))
                        {
                            var sourcePath = sourceObj?.ToString();

                            // Remove "!file " prefix if present
                            if (sourcePath?.StartsWith("!file ") == true)
                            {
                                sourcePath = sourcePath.Substring(6);
                            }

                            if (!string.IsNullOrEmpty(sourcePath))
                            {
                                // Source is relative to the .sdsheet file location
                                var assetDir = Path.GetDirectoryName(asset.FilePath);
                                if (assetDir == null) return null;

                                return Path.GetFullPath(Path.Combine(assetDir, sourcePath));
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback to old method
            }

            return null;
        }

        /// <summary>
        /// Load a BitmapImage from a file path
        /// </summary>
        private ImageSource LoadBitmapImage(string filePath)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }

        /// <summary>
        /// Get the dimensions of an image asset, scaled to fit within maxSize while maintaining aspect ratio
        /// </summary>
        public (double Width, double Height)? GetScaledImageDimensions(string assetName, double maxSize = 200)
        {
            if (_connectedProject == null || string.IsNullOrEmpty(assetName))
                return null;

            try
            {
                // Load the image to get its dimensions
                var imageSource = LoadAssetImage(assetName);
                if (imageSource?.NativeSource is BitmapSource bitmap)
                {
                    double originalWidth = bitmap.PixelWidth;
                    double originalHeight = bitmap.PixelHeight;

                    // Calculate scale to fit within maxSize
                    double scale = 1.0;
                    if (originalWidth > originalHeight)
                    {
                        // Width is longer
                        if (originalWidth > maxSize)
                            scale = maxSize / originalWidth;
                    }
                    else
                    {
                        // Height is longer or equal
                        if (originalHeight > maxSize)
                            scale = maxSize / originalHeight;
                    }

                    return (originalWidth * scale, originalHeight * scale);
                }
            }
            catch
            {
                // Failed to load image
            }

            return null;
        }

        #endregion

        #region Asset Queries

        /// <summary>
        /// Find an asset by name
        /// </summary>
        public object? FindAsset(string assetName)
        {
            if (_connectedProject == null)
                return null;

            try
            {
                return _connectedProject.FindAsset(assetName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Check if an asset exists
        /// </summary>
        public bool AssetExists(string assetName)
        {
            return FindAsset(assetName) != null;
        }

        #endregion

        #region Sprite Frame Operations

        /// <summary>
        /// Get a specific sprite frame from a sprite sheet by frame index
        /// Returns the cropped image for just that frame
        /// </summary>
        public IImageSource? GetSpriteFrame(string assetReference, int frameIndex)
        {
            if (_connectedProject == null || string.IsNullOrEmpty(assetReference))
                return null;

            try
            {
                // Parse asset reference to get the asset
                var cleanName = assetReference;
                string? guid = null;
                if (assetReference.Contains(':'))
                {
                    guid = assetReference.Split(':').First();
                    cleanName = assetReference.Split(':').Last();
                    cleanName = Path.GetFileNameWithoutExtension(cleanName);
                }

                // Find the asset
                var asset = _connectedProject.FindAsset(cleanName);
                if (asset == null && guid != null)
                {
                    asset = _connectedProject.FindAssetByGuid(guid);
                }

                if (asset == null) return null;

                // Only sprite sheets have frames
                if (asset.Type != AssetType.SpriteSheet)
                {
                    // For textures, just return the whole image
                    return LoadAssetImage(assetReference);
                }

                // Get the sprite sheet and find the frame's TextureRegion
                var spriteSheetAsset = _connectedProject.LoadSpriteSheet(asset.Name);
                var allProps = spriteSheetAsset.GetAllProperties();

                if (!allProps.TryGetValue("Sprites", out var spritesObj) ||
                    spritesObj is not Dictionary<string, object> sprites)
                {
                    return LoadAssetImage(assetReference);
                }

                // Get the sprite at the specified frame index
                var spriteList = sprites.Values.ToList();
                if (frameIndex < 0 || frameIndex >= spriteList.Count)
                {
                    return LoadAssetImage(assetReference);
                }

                var spriteData = spriteList[frameIndex] as Dictionary<string, object>;
                if (spriteData == null)
                {
                    return LoadAssetImage(assetReference);
                }

                // Get TextureRegion for cropping
                if (!spriteData.TryGetValue("TextureRegion", out var regionObj) ||
                    regionObj is not Dictionary<string, object> region)
                {
                    return LoadAssetImage(assetReference);
                }

                var regionX = Convert.ToInt32(region.GetValueOrDefault("X", 0));
                var regionY = Convert.ToInt32(region.GetValueOrDefault("Y", 0));
                var regionWidth = Convert.ToInt32(region.GetValueOrDefault("Width", 0));
                var regionHeight = Convert.ToInt32(region.GetValueOrDefault("Height", 0));

                if (regionWidth <= 0 || regionHeight <= 0)
                {
                    return LoadAssetImage(assetReference);
                }

                // Get the source image path
                var imagePath = GetSpriteSheetSourcePath(asset);
                if (imagePath == null || !File.Exists(imagePath))
                {
                    return null;
                }

                // Load and crop the image
                var croppedImage = LoadAndCropImage(imagePath, regionX, regionY, regionWidth, regionHeight);
                if (croppedImage != null)
                {
                    return new WpfImageSource(croppedImage);
                }
            }
            catch
            {
                // Fall back to full image on error
            }

            return LoadAssetImage(assetReference);
        }

        /// <summary>
        /// Load an image and crop it to the specified region
        /// </summary>
        private ImageSource? LoadAndCropImage(string filePath, int x, int y, int width, int height)
        {
            try
            {
                var originalBitmap = new BitmapImage();
                originalBitmap.BeginInit();
                originalBitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                originalBitmap.CacheOption = BitmapCacheOption.OnLoad;
                originalBitmap.EndInit();

                // Create a cropped bitmap
                var croppedBitmap = new CroppedBitmap(
                    originalBitmap,
                    new Int32Rect(x, y, width, height));

                return croppedBitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Apply a tint color to an image by multiplying pixel colors (like Stride's Color property)
        /// </summary>
        public ImageSource? ApplyTintToImage(ImageSource source, System.Windows.Media.Color tintColor)
        {
            if (source == null) return null;

            // If tint is white (255,255,255,255), no tinting needed
            if (tintColor.R == 255 && tintColor.G == 255 && tintColor.B == 255 && tintColor.A == 255)
                return source;

            try
            {
                // Convert to BitmapSource if needed
                BitmapSource bitmapSource;
                if (source is BitmapSource bs)
                {
                    bitmapSource = bs;
                }
                else
                {
                    return source; // Can't tint non-bitmap sources
                }

                // Convert to a format we can work with (Bgra32)
                var converted = new FormatConvertedBitmap(bitmapSource, PixelFormats.Bgra32, null, 0);

                int width = converted.PixelWidth;
                int height = converted.PixelHeight;
                int stride = width * 4; // 4 bytes per pixel (BGRA)
                byte[] pixels = new byte[height * stride];
                converted.CopyPixels(pixels, stride, 0);

                // Multiply each pixel by the tint color
                float tR = tintColor.R / 255f;
                float tG = tintColor.G / 255f;
                float tB = tintColor.B / 255f;
                float tA = tintColor.A / 255f;

                for (int i = 0; i < pixels.Length; i += 4)
                {
                    // BGRA format
                    byte b = pixels[i];
                    byte g = pixels[i + 1];
                    byte r = pixels[i + 2];
                    byte a = pixels[i + 3];

                    // Multiply by tint (Stride's multiplicative blend)
                    pixels[i] = (byte)(b * tB);         // B
                    pixels[i + 1] = (byte)(g * tG);     // G
                    pixels[i + 2] = (byte)(r * tR);     // R
                    pixels[i + 3] = (byte)(a * tA);     // A
                }

                // Create new bitmap with tinted pixels
                var result = BitmapSource.Create(
                    width, height,
                    converted.DpiX, converted.DpiY,
                    PixelFormats.Bgra32, null,
                    pixels, stride);

                result.Freeze(); // Make it thread-safe
                return result;
            }
            catch
            {
                return source; // Return original on error
            }
        }

        #endregion

        #region Font Loading

        /// <summary>
        /// Get all fonts in the project
        /// </summary>
        public List<FontInfo> GetProjectFonts()
        {
            if (_connectedProject == null)
                return new List<FontInfo>();

            // Return cached fonts if available
            if (_cachedFonts != null)
                return _cachedFonts.Values.ToList();

            _cachedFonts = new Dictionary<string, FontInfo>();

            try
            {
                // Get all SpriteFont assets (.sdfnt files)
                var fontAssets = _connectedProject.GetAssets(AssetType.SpriteFont);

                foreach (var fontAsset in fontAssets)
                {
                    var fontInfo = LoadFontFromAsset(fontAsset);
                    if (fontInfo != null)
                    {
                        _cachedFonts[fontInfo.AssetReference] = fontInfo;
                    }
                }
            }
            catch
            {
                // Silently fail if fonts can't be loaded
            }

            return _cachedFonts.Values.ToList();
        }

        /// <summary>
        /// Load a font from an asset reference
        /// </summary>
        public System.Windows.Media.FontFamily? LoadFont(string fontAssetReference)
        {
            var fontInfo = GetFontInfo(fontAssetReference);
            return fontInfo?.FontFamily;
        }

        /// <summary>
        /// Get font info from a font asset reference
        /// </summary>
        public FontInfo? GetFontInfo(string fontAssetReference)
        {
            if (string.IsNullOrEmpty(fontAssetReference) || _connectedProject == null)
                return null;

            // Ensure fonts are loaded
            if (_cachedFonts == null)
                GetProjectFonts();

            // Try direct lookup first
            if (_cachedFonts!.TryGetValue(fontAssetReference, out var cachedFont))
                return cachedFont;

            // Try parsing the reference and finding by name or guid
            try
            {
                var cleanName = fontAssetReference;
                string? guid = null;

                if (fontAssetReference.Contains(':'))
                {
                    guid = fontAssetReference.Split(':').First();
                    cleanName = fontAssetReference.Split(':').Last();
                    cleanName = Path.GetFileNameWithoutExtension(cleanName);
                }

                // Try to find by name
                var matchByName = _cachedFonts.Values.FirstOrDefault(f =>
                    f.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase));
                if (matchByName != null)
                    return matchByName;

                // Try to find by guid in the reference
                if (guid != null)
                {
                    var matchByGuid = _cachedFonts.Values.FirstOrDefault(f =>
                        f.AssetReference.StartsWith(guid));
                    if (matchByGuid != null)
                        return matchByGuid;
                }

                // Last resort: try to load the asset directly
                var asset = _connectedProject.FindAsset(cleanName);
                if (asset == null && guid != null)
                    asset = _connectedProject.FindAssetByGuid(guid);

                if (asset != null && asset.Type == AssetType.SpriteFont)
                {
                    var fontInfo = LoadFontFromAsset(asset);
                    if (fontInfo != null)
                    {
                        _cachedFonts[fontInfo.AssetReference] = fontInfo;
                        return fontInfo;
                    }
                }
            }
            catch
            {
                // Failed to load font
            }

            return null;
        }

        /// <summary>
        /// Load font information from a font asset using toolkit's GetAssetSource
        /// </summary>
        private FontInfo? LoadFontFromAsset(AssetReference fontAsset)
        {
            try
            {
                var fontInfo = new FontInfo
                {
                    AssetReference = $"{fontAsset.Id}:{fontAsset.Path}",
                    Name = fontAsset.Name
                };

                // Use toolkit's GetAssetSource to get the font file path
                var fontFilePath = _connectedProject!.GetAssetSource(fontAsset);

                if (!string.IsNullOrEmpty(fontFilePath) && File.Exists(fontFilePath))
                {
                    // File-based font (FileFontProvider)
                    fontInfo.FontFilePath = fontFilePath;
                    fontInfo.IsFileBased = true;
                    fontInfo.FontFamily = LoadFontFamilyFromFile(fontFilePath);
                    fontInfo.FontFamilyName = Path.GetFileNameWithoutExtension(fontFilePath);
                }
                else
                {
                    // System font (SystemFontProvider) - try to get FontName from the asset
                    var systemFontName = GetSystemFontName(fontAsset.FilePath);
                    if (!string.IsNullOrEmpty(systemFontName))
                    {
                        fontInfo.FontFamilyName = systemFontName;
                        fontInfo.IsFileBased = false;
                        fontInfo.FontFamily = new System.Windows.Media.FontFamily(systemFontName);
                    }
                }

                // Fallback to Arial if we couldn't determine the font
                if (fontInfo.FontFamily == null)
                {
                    fontInfo.FontFamily = new System.Windows.Media.FontFamily("Arial");
                    fontInfo.FontFamilyName = "Arial";
                }

                return fontInfo;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get the system font name from a .sdfnt file (for SystemFontProvider)
        /// </summary>
        private string? GetSystemFontName(string sdfntFilePath)
        {
            try
            {
                if (!File.Exists(sdfntFilePath))
                    return null;

                var content = File.ReadAllText(sdfntFilePath);
                var lines = content.Split('\n');

                bool inFontSource = false;
                foreach (var line in lines)
                {
                    var trimmedLine = line.TrimStart();

                    if (trimmedLine.StartsWith("FontSource:"))
                    {
                        // Check if it's a SystemFontProvider
                        if (trimmedLine.Contains("!SystemFontProvider"))
                        {
                            inFontSource = true;
                            continue;
                        }
                        else
                        {
                            // Not a system font
                            return null;
                        }
                    }

                    if (inFontSource && trimmedLine.StartsWith("FontName:"))
                    {
                        return trimmedLine.Substring("FontName:".Length).Trim();
                    }

                    // Exit if we hit a non-indented line
                    if (inFontSource && !string.IsNullOrWhiteSpace(line) && !char.IsWhiteSpace(line[0]))
                    {
                        break;
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return null;
        }

        /// <summary>
        /// Load a WPF FontFamily from a font file (.ttf, .otf)
        /// </summary>
        private System.Windows.Media.FontFamily? LoadFontFamilyFromFile(string fontFilePath)
        {
            try
            {
                if (!File.Exists(fontFilePath))
                    return null;

                // Get the directory containing the font file
                var fontDir = Path.GetDirectoryName(fontFilePath);
                if (fontDir == null) return null;

                var fontFileName = Path.GetFileName(fontFilePath);

                // Try to load the GlyphTypeface directly to get the actual font family name
                try
                {
                    var fontUri = new Uri(fontFilePath);
                    var glyphTypeface = new System.Windows.Media.GlyphTypeface(fontUri);
                    var familyNames = glyphTypeface.FamilyNames;

                    // Try to get English name, fall back to first available
                    string? familyName = null;
                    var enUS = new System.Globalization.CultureInfo("en-US");
                    if (familyNames.TryGetValue(enUS, out var englishName))
                    {
                        familyName = englishName;
                    }
                    else if (familyNames.Count > 0)
                    {
                        familyName = familyNames.Values.First();
                    }

                    if (!string.IsNullOrEmpty(familyName))
                    {
                        // Create FontFamily using the directory URI and the discovered family name
                        var dirUri = new Uri(fontDir + "/", UriKind.Absolute);
                        return new System.Windows.Media.FontFamily(dirUri, $"./{fontFileName}#{familyName}");
                    }
                }
                catch
                {
                    // GlyphTypeface failed, try fallback methods
                }

                // Fallback: try using Fonts.GetFontFamilies on the directory
                var directoryUri = new Uri(fontDir + "/", UriKind.Absolute);
                var families = System.Windows.Media.Fonts.GetFontFamilies(directoryUri);

                foreach (var family in families)
                {
                    var source = family.Source;
                    if (source.Contains(fontFileName, StringComparison.OrdinalIgnoreCase) ||
                        source.Contains(Path.GetFileNameWithoutExtension(fontFileName), StringComparison.OrdinalIgnoreCase))
                    {
                        return family;
                    }
                }

                // Last resort: return first family if any exist
                if (families.Any())
                    return families.First();

                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}

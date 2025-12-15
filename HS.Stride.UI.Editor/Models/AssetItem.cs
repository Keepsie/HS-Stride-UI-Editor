// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows.Media;

namespace HS.Stride.UI.Editor.Models
{
    /// <summary>
    /// Model for displaying asset items in the UI
    /// </summary>
    public class AssetItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty; // Relative path from Assets folder (e.g., "UI/Textures/image")
        public string Type { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public object? AssetReference { get; set; }
        public ImageSource? Thumbnail { get; set; }
    }
}

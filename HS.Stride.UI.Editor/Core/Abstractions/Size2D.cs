// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace HS.Stride.UI.Editor.Core.Abstractions
{
    /// <summary>
    /// Platform-agnostic 2D size structure for testability
    /// </summary>
    public struct Size2D
    {
        public double Width { get; init; }
        public double Height { get; init; }

        public Size2D(double width, double height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Convert from WPF Size
        /// </summary>
        public static Size2D FromWpf(System.Windows.Size size)
            => new(size.Width, size.Height);

        /// <summary>
        /// Convert to WPF Size
        /// </summary>
        public System.Windows.Size ToWpf()
            => new(Width, Height);

        public override string ToString() => $"{Width}×{Height}";
    }
}

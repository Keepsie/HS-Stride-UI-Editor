// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace HS.Stride.UI.Editor.Core.Abstractions
{
    /// <summary>
    /// Platform-agnostic 2D rectangle structure for testability
    /// </summary>
    public struct Rect2D
    {
        public double X { get; init; }
        public double Y { get; init; }
        public double Width { get; init; }
        public double Height { get; init; }

        public Rect2D(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public double Left => X;
        public double Top => Y;
        public double Right => X + Width;
        public double Bottom => Y + Height;

        /// <summary>
        /// Convert from WPF Rect
        /// </summary>
        public static Rect2D FromWpf(System.Windows.Rect rect)
            => new(rect.X, rect.Y, rect.Width, rect.Height);

        /// <summary>
        /// Convert to WPF Rect
        /// </summary>
        public System.Windows.Rect ToWpf()
            => new(X, Y, Width, Height);

        public override string ToString() => $"[{X}, {Y}, {Width}×{Height}]";
    }
}

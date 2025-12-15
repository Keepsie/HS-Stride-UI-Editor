// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace HS.Stride.UI.Editor.Core.Abstractions
{
    /// <summary>
    /// Platform-agnostic 2D point structure for testability
    /// </summary>
    public struct Point2D
    {
        public double X { get; init; }
        public double Y { get; init; }

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Convert from WPF Point
        /// </summary>
        public static Point2D FromWpf(System.Windows.Point point)
            => new(point.X, point.Y);

        /// <summary>
        /// Convert to WPF Point
        /// </summary>
        public System.Windows.Point ToWpf()
            => new(X, Y);

        public override string ToString() => $"({X}, {Y})";
    }
}

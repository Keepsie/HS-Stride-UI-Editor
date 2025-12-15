// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace HS.Stride.UI.Editor.Core.Abstractions
{
    /// <summary>
    /// Platform-agnostic image source abstraction for testability
    /// </summary>
    public interface IImageSource
    {
        double Width { get; }
        double Height { get; }

        /// <summary>
        /// Gets the native platform image source (for WPF binding)
        /// </summary>
        object NativeSource { get; }
    }
}

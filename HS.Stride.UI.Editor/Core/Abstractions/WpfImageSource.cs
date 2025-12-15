// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows.Media;

namespace HS.Stride.UI.Editor.Core.Abstractions
{
    /// <summary>
    /// WPF implementation of IImageSource
    /// </summary>
    public class WpfImageSource : IImageSource
    {
        private readonly ImageSource _wpfSource;

        public WpfImageSource(ImageSource wpfSource)
        {
            _wpfSource = wpfSource;
        }

        public double Width => _wpfSource.Width;
        public double Height => _wpfSource.Height;
        public object NativeSource => _wpfSource;

        /// <summary>
        /// Implicit conversion from WPF ImageSource
        /// </summary>
        public static implicit operator WpfImageSource?(ImageSource? source)
            => source != null ? new WpfImageSource(source) : null;

        /// <summary>
        /// Explicit conversion to WPF ImageSource
        /// </summary>
        public static explicit operator ImageSource(WpfImageSource source)
            => (ImageSource)source.NativeSource;
    }
}

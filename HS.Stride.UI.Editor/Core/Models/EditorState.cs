// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HS.Stride.UI.Editor.Core.Models
{
    /// <summary>
    /// Encapsulates the current editor state (zoom, pan, grid settings, etc.)
    /// </summary>
    public class EditorState : INotifyPropertyChanged
    {
        private double _zoomLevel = 1.0;
        private double _designWidth = 1280;
        private double _designHeight = 720;
        private bool _snapToGrid = true;
        private bool _snapToPixel = true;
        private double _gridSize = 10.0;

        public const double ZoomMin = 0.1;
        public const double ZoomMax = 5.0;
        public const double ZoomStep = 0.1;

        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                if (_zoomLevel != value)
                {
                    _zoomLevel = Math.Max(ZoomMin, Math.Min(ZoomMax, value));
                    OnPropertyChanged();
                }
            }
        }

        public double DesignWidth
        {
            get => _designWidth;
            set
            {
                if (_designWidth != value)
                {
                    _designWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        public double DesignHeight
        {
            get => _designHeight;
            set
            {
                if (_designHeight != value)
                {
                    _designHeight = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool SnapToGrid
        {
            get => _snapToGrid;
            set
            {
                if (_snapToGrid != value)
                {
                    _snapToGrid = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool SnapToPixel
        {
            get => _snapToPixel;
            set
            {
                if (_snapToPixel != value)
                {
                    _snapToPixel = value;
                    OnPropertyChanged();
                }
            }
        }

        public double GridSize
        {
            get => _gridSize;
            set
            {
                if (_gridSize != value && value > 0)
                {
                    _gridSize = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

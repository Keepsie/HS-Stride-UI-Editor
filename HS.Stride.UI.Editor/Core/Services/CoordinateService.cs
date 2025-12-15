// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using HS.Stride.UI.Editor.Core.Models;

namespace HS.Stride.UI.Editor.Core.Services
{
    /// <summary>
    /// Handles all coordinate transformations, snapping, and artboard calculations
    /// </summary>
    public class CoordinateService
    {
        private readonly EditorState _editorState;
        private Point _artboardOffset;

        public CoordinateService(EditorState editorState)
        {
            _editorState = editorState;
        }

        #region Snapping

        /// <summary>
        /// Snap a value to the grid based on editor settings
        /// </summary>
        public double SnapToGrid(double value)
        {
            if (!_editorState.SnapToGrid)
                return _editorState.SnapToPixel ? Math.Round(value) : value;

            var snap = _editorState.GridSize;
            var snapped = Math.Round(value / snap) * snap;
            return _editorState.SnapToPixel ? Math.Round(snapped) : snapped;
        }

        /// <summary>
        /// Snap a point to the grid based on editor settings
        /// </summary>
        public Point SnapToGrid(Point point)
        {
            return new Point(SnapToGrid(point.X), SnapToGrid(point.Y));
        }

        /// <summary>
        /// Snap a size (width/height) to the grid
        /// </summary>
        public Size SnapToGrid(Size size)
        {
            return new Size(SnapToGrid(size.Width), SnapToGrid(size.Height));
        }

        #endregion

        #region Artboard Calculations

        /// <summary>
        /// Set the artboard offset (position of artboard on canvas)
        /// </summary>
        public void SetArtboardOffset(double x, double y)
        {
            _artboardOffset = new Point(x, y);
        }

        /// <summary>
        /// Get the artboard offset (position of artboard on canvas)
        /// </summary>
        public Point GetArtboardOffset()
        {
            return _artboardOffset;
        }

        /// <summary>
        /// Get the artboard bounds (design size)
        /// </summary>
        public Size GetArtboardBounds()
        {
            return new Size(_editorState.DesignWidth, _editorState.DesignHeight);
        }

        /// <summary>
        /// Get the artboard rectangle (with offset and size)
        /// </summary>
        public Rect GetArtboardRect()
        {
            return new Rect(_artboardOffset, GetArtboardBounds());
        }

        #endregion

        #region Coordinate Transformations

        /// <summary>
        /// Convert canvas coordinates to artboard-relative coordinates
        /// </summary>
        public Point CanvasToArtboard(Point canvasPoint)
        {
            return new Point(
                canvasPoint.X - _artboardOffset.X,
                canvasPoint.Y - _artboardOffset.Y
            );
        }

        /// <summary>
        /// Convert artboard-relative coordinates to canvas coordinates
        /// </summary>
        public Point ArtboardToCanvas(Point artboardPoint)
        {
            return new Point(
                artboardPoint.X + _artboardOffset.X,
                artboardPoint.Y + _artboardOffset.Y
            );
        }

        /// <summary>
        /// Clamp a point to stay within artboard bounds
        /// </summary>
        public Point ClampToArtboard(Point point, Size elementSize)
        {
            var bounds = GetArtboardBounds();
            var x = Math.Max(0, Math.Min(bounds.Width - elementSize.Width, point.X));
            var y = Math.Max(0, Math.Min(bounds.Height - elementSize.Height, point.Y));
            return new Point(x, y);
        }

        /// <summary>
        /// Clamp a rectangle to stay within artboard bounds
        /// </summary>
        public Rect ClampToArtboard(Rect rect)
        {
            var bounds = GetArtboardBounds();
            var x = Math.Max(0, Math.Min(bounds.Width - rect.Width, rect.X));
            var y = Math.Max(0, Math.Min(bounds.Height - rect.Height, rect.Y));
            var width = Math.Min(rect.Width, bounds.Width - x);
            var height = Math.Min(rect.Height, bounds.Height - y);
            return new Rect(x, y, width, height);
        }

        #endregion

        #region World to Local Transformations

        /// <summary>
        /// Transform a point from world (artboard) coordinates to local (parent) coordinates
        /// Used when reparenting elements
        /// </summary>
        public Point WorldToLocal(Point worldPoint, Point parentWorldPosition)
        {
            return new Point(
                worldPoint.X - parentWorldPosition.X,
                worldPoint.Y - parentWorldPosition.Y
            );
        }

        /// <summary>
        /// Transform a point from local (parent) coordinates to world (artboard) coordinates
        /// </summary>
        public Point LocalToWorld(Point localPoint, Point parentWorldPosition)
        {
            return new Point(
                localPoint.X + parentWorldPosition.X,
                localPoint.Y + parentWorldPosition.Y
            );
        }

        /// <summary>
        /// Calculate the world position of an element (accumulates parent positions)
        /// </summary>
        public Point GetWorldPosition(double localX, double localY, ViewModels.UIElementViewModel? parent)
        {
            var worldPos = new Point(localX, localY);
            var currentParent = parent;

            while (currentParent != null && !currentParent.IsSystemElement)
            {
                worldPos.X += currentParent.X;
                worldPos.Y += currentParent.Y;
                currentParent = currentParent.Parent;
            }

            return worldPos;
        }

        #endregion

        #region Zoom Helpers

        /// <summary>
        /// Get the current zoom level
        /// </summary>
        public double GetZoomLevel()
        {
            return _editorState.ZoomLevel;
        }

        /// <summary>
        /// Apply zoom to a value (for visual rendering)
        /// </summary>
        public double ApplyZoom(double value)
        {
            return value * _editorState.ZoomLevel;
        }

        /// <summary>
        /// Remove zoom from a value (for coordinate calculations)
        /// </summary>
        public double RemoveZoom(double value)
        {
            return value / _editorState.ZoomLevel;
        }

        #endregion
    }
}

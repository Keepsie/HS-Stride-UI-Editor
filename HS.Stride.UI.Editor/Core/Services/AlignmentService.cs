// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using HS.Stride.UI.Editor.ViewModels;
using HS.Stride.UI.Editor.Models.Commands;

namespace HS.Stride.UI.Editor.Core.Services
{
    /// <summary>
    /// Provides alignment and distribution operations for selected UI elements.
    /// Returns commands that can be executed via UndoRedoManager.
    /// </summary>
    public class AlignmentService
    {
        /// <summary>
        /// Align elements to the leftmost edge
        /// </summary>
        public BatchMoveCommand? AlignLeft(List<UIElementViewModel> elements)
        {
            if (elements.Count < 2) return null;

            var minX = elements.Min(el => el.X);
            var moves = elements
                .Select(el => (el, el.X, el.Y, minX, el.Y))
                .ToList();

            return new BatchMoveCommand(moves, "Align Left");
        }

        /// <summary>
        /// Align elements to horizontal center
        /// </summary>
        public BatchMoveCommand? AlignCenterH(List<UIElementViewModel> elements)
        {
            if (elements.Count < 2) return null;

            var minX = elements.Min(el => el.X);
            var maxX = elements.Max(el => el.X + el.Width);
            var centerX = (minX + maxX) / 2;

            var moves = elements
                .Select(el => (el, el.X, el.Y, centerX - el.Width / 2, el.Y))
                .ToList();

            return new BatchMoveCommand(moves, "Align Center Horizontal");
        }

        /// <summary>
        /// Align elements to the rightmost edge
        /// </summary>
        public BatchMoveCommand? AlignRight(List<UIElementViewModel> elements)
        {
            if (elements.Count < 2) return null;

            var maxX = elements.Max(el => el.X + el.Width);
            var moves = elements
                .Select(el => (el, el.X, el.Y, maxX - el.Width, el.Y))
                .ToList();

            return new BatchMoveCommand(moves, "Align Right");
        }

        /// <summary>
        /// Align elements to the topmost edge
        /// </summary>
        public BatchMoveCommand? AlignTop(List<UIElementViewModel> elements)
        {
            if (elements.Count < 2) return null;

            var minY = elements.Min(el => el.Y);
            var moves = elements
                .Select(el => (el, el.X, el.Y, el.X, minY))
                .ToList();

            return new BatchMoveCommand(moves, "Align Top");
        }

        /// <summary>
        /// Align elements to vertical center
        /// </summary>
        public BatchMoveCommand? AlignCenterV(List<UIElementViewModel> elements)
        {
            if (elements.Count < 2) return null;

            var minY = elements.Min(el => el.Y);
            var maxY = elements.Max(el => el.Y + el.Height);
            var centerY = (minY + maxY) / 2;

            var moves = elements
                .Select(el => (el, el.X, el.Y, el.X, centerY - el.Height / 2))
                .ToList();

            return new BatchMoveCommand(moves, "Align Center Vertical");
        }

        /// <summary>
        /// Align elements to the bottommost edge
        /// </summary>
        public BatchMoveCommand? AlignBottom(List<UIElementViewModel> elements)
        {
            if (elements.Count < 2) return null;

            var maxY = elements.Max(el => el.Y + el.Height);
            var moves = elements
                .Select(el => (el, el.X, el.Y, el.X, maxY - el.Height))
                .ToList();

            return new BatchMoveCommand(moves, "Align Bottom");
        }

        /// <summary>
        /// Distribute elements evenly horizontally (requires 3+ elements)
        /// </summary>
        public BatchMoveCommand? DistributeH(List<UIElementViewModel> elements)
        {
            if (elements.Count < 3) return null;

            var sorted = elements.OrderBy(el => el.X).ToList();
            var first = sorted.First();
            var last = sorted.Last();

            var totalWidth = (last.X + last.Width) - first.X;
            var elementsWidth = sorted.Sum(el => el.Width);
            var gap = (totalWidth - elementsWidth) / (sorted.Count - 1);

            var moves = new List<(UIElementViewModel, double, double, double, double)>();
            double currentX = first.X + first.Width + gap;

            for (int i = 1; i < sorted.Count - 1; i++)
            {
                moves.Add((sorted[i], sorted[i].X, sorted[i].Y, currentX, sorted[i].Y));
                currentX += sorted[i].Width + gap;
            }

            return moves.Count > 0 ? new BatchMoveCommand(moves, "Distribute Horizontal") : null;
        }

        /// <summary>
        /// Distribute elements evenly vertically (requires 3+ elements)
        /// </summary>
        public BatchMoveCommand? DistributeV(List<UIElementViewModel> elements)
        {
            if (elements.Count < 3) return null;

            var sorted = elements.OrderBy(el => el.Y).ToList();
            var first = sorted.First();
            var last = sorted.Last();

            var totalHeight = (last.Y + last.Height) - first.Y;
            var elementsHeight = sorted.Sum(el => el.Height);
            var gap = (totalHeight - elementsHeight) / (sorted.Count - 1);

            var moves = new List<(UIElementViewModel, double, double, double, double)>();
            double currentY = first.Y + first.Height + gap;

            for (int i = 1; i < sorted.Count - 1; i++)
            {
                moves.Add((sorted[i], sorted[i].X, sorted[i].Y, sorted[i].X, currentY));
                currentY += sorted[i].Height + gap;
            }

            return moves.Count > 0 ? new BatchMoveCommand(moves, "Distribute Vertical") : null;
        }

        /// <summary>
        /// Reset elements to position (0, 0)
        /// </summary>
        public BatchMoveCommand? ResetPosition(List<UIElementViewModel> elements)
        {
            if (elements.Count == 0) return null;

            var moves = elements
                .Select(el => (el, el.X, el.Y, 0.0, 0.0))
                .ToList();

            return new BatchMoveCommand(moves, "Reset Position");
        }

        /// <summary>
        /// Reset elements to default size (200x100)
        /// </summary>
        public BatchResizeCommand? ResetSize(List<UIElementViewModel> elements)
        {
            if (elements.Count == 0) return null;

            var resizes = elements
                .Select(el => (el, el.X, el.Y, el.Width, el.Height, el.X, el.Y, 200.0, 100.0))
                .ToList();

            return new BatchResizeCommand(resizes, "Reset Size");
        }

        // ===== Parent Alignment Methods (for single child element) =====

        /// <summary>
        /// Align element to the left edge of its parent
        /// </summary>
        public BatchMoveCommand? AlignToParentLeft(UIElementViewModel element)
        {
            if (element.Parent == null || element.Parent.IsSystemElement) return null;

            var moves = new List<(UIElementViewModel, double, double, double, double)>
            {
                (element, element.X, element.Y, 0, element.Y)
            };

            return new BatchMoveCommand(moves, "Align to Parent Left");
        }

        /// <summary>
        /// Align element to horizontal center of its parent
        /// </summary>
        public BatchMoveCommand? AlignToParentCenterH(UIElementViewModel element)
        {
            if (element.Parent == null || element.Parent.IsSystemElement) return null;

            var parentWidth = element.Parent.Width;
            var newX = (parentWidth - element.Width) / 2;

            var moves = new List<(UIElementViewModel, double, double, double, double)>
            {
                (element, element.X, element.Y, newX, element.Y)
            };

            return new BatchMoveCommand(moves, "Align to Parent Center (H)");
        }

        /// <summary>
        /// Align element to the right edge of its parent
        /// </summary>
        public BatchMoveCommand? AlignToParentRight(UIElementViewModel element)
        {
            if (element.Parent == null || element.Parent.IsSystemElement) return null;

            var parentWidth = element.Parent.Width;
            var newX = parentWidth - element.Width;

            var moves = new List<(UIElementViewModel, double, double, double, double)>
            {
                (element, element.X, element.Y, newX, element.Y)
            };

            return new BatchMoveCommand(moves, "Align to Parent Right");
        }

        /// <summary>
        /// Align element to the top edge of its parent
        /// </summary>
        public BatchMoveCommand? AlignToParentTop(UIElementViewModel element)
        {
            if (element.Parent == null || element.Parent.IsSystemElement) return null;

            var moves = new List<(UIElementViewModel, double, double, double, double)>
            {
                (element, element.X, element.Y, element.X, 0)
            };

            return new BatchMoveCommand(moves, "Align to Parent Top");
        }

        /// <summary>
        /// Align element to vertical center of its parent
        /// </summary>
        public BatchMoveCommand? AlignToParentCenterV(UIElementViewModel element)
        {
            if (element.Parent == null || element.Parent.IsSystemElement) return null;

            var parentHeight = element.Parent.Height;
            var newY = (parentHeight - element.Height) / 2;

            var moves = new List<(UIElementViewModel, double, double, double, double)>
            {
                (element, element.X, element.Y, element.X, newY)
            };

            return new BatchMoveCommand(moves, "Align to Parent Center (V)");
        }

        /// <summary>
        /// Align element to the bottom edge of its parent
        /// </summary>
        public BatchMoveCommand? AlignToParentBottom(UIElementViewModel element)
        {
            if (element.Parent == null || element.Parent.IsSystemElement) return null;

            var parentHeight = element.Parent.Height;
            var newY = parentHeight - element.Height;

            var moves = new List<(UIElementViewModel, double, double, double, double)>
            {
                (element, element.X, element.Y, element.X, newY)
            };

            return new BatchMoveCommand(moves, "Align to Parent Bottom");
        }
    }
}

// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Models.Commands
{
    /// <summary>
    /// Command for creating a new element
    /// </summary>
    public class CreateElementCommand : IUndoableCommand
    {
        private readonly UIElementViewModel _element;
        private readonly UIElementViewModel? _parent;
        private readonly ObservableCollection<UIElementViewModel> _rootElements;
        private readonly Action<UIElementViewModel> _renderElement;
        private readonly Action<UIElementViewModel> _removeVisual;
        private readonly Action<UIElementViewModel> _selectElement;

        public string Description => $"Create {_element.ElementType}";

        public CreateElementCommand(
            UIElementViewModel element,
            UIElementViewModel? parent,
            ObservableCollection<UIElementViewModel> rootElements,
            Action<UIElementViewModel> renderElement,
            Action<UIElementViewModel> removeVisual,
            Action<UIElementViewModel> selectElement)
        {
            _element = element;
            _parent = parent;
            _rootElements = rootElements;
            _renderElement = renderElement;
            _removeVisual = removeVisual;
            _selectElement = selectElement;
        }

        public void Execute()
        {
            if (_parent != null)
            {
                _element.Parent = _parent;
                _parent.Children.Add(_element);

                // Set ZIndex based on position in siblings (last = highest = on top)
                _element.ZIndex = _parent.Children.Count - 1;
            }
            else
            {
                _element.Parent = null;
                _rootElements.Add(_element);

                // Set ZIndex for root elements too
                _element.ZIndex = _rootElements.Count - 1;
            }

            _renderElement(_element);
            _selectElement(_element);
        }

        public void Undo()
        {
            _removeVisual(_element);

            if (_parent != null)
            {
                _parent.Children.Remove(_element);
                _element.Parent = null;
            }
            else
            {
                _rootElements.Remove(_element);
                _element.Parent = null;
            }
        }
    }

    /// <summary>
    /// Command for deleting an element
    /// </summary>
    public class DeleteElementCommand : IUndoableCommand
    {
        private readonly UIElementViewModel _element;
        private readonly UIElementViewModel? _parent;
        private readonly ObservableCollection<UIElementViewModel> _rootElements;
        private readonly int _index;
        private readonly Action<UIElementViewModel> _renderElement;
        private readonly Action<UIElementViewModel> _removeVisual;

        public string Description => $"Delete {_element.Name}";

        public DeleteElementCommand(
            UIElementViewModel element,
            UIElementViewModel? parent,
            ObservableCollection<UIElementViewModel> rootElements,
            Action<UIElementViewModel> renderElement,
            Action<UIElementViewModel> removeVisual)
        {
            _element = element;
            _parent = parent;
            _rootElements = rootElements;
            _renderElement = renderElement;
            _removeVisual = removeVisual;

            // Store index for undo
            if (_parent != null)
            {
                _index = _parent.Children.IndexOf(_element);
            }
            else
            {
                _index = _rootElements.IndexOf(_element);
            }
        }

        public void Execute()
        {
            _removeVisual(_element);

            if (_parent != null)
            {
                _parent.Children.Remove(_element);
                _element.Parent = null;
            }
            else
            {
                _rootElements.Remove(_element);
                _element.Parent = null;
            }
        }

        public void Undo()
        {
            if (_parent != null)
            {
                _element.Parent = _parent;
                if (_index >= 0 && _index <= _parent.Children.Count)
                {
                    _parent.Children.Insert(_index, _element);
                }
                else
                {
                    _parent.Children.Add(_element);
                }
            }
            else
            {
                _element.Parent = null;
                if (_index >= 0 && _index <= _rootElements.Count)
                {
                    _rootElements.Insert(_index, _element);
                }
                else
                {
                    _rootElements.Add(_element);
                }
            }

            _renderElement(_element);
        }
    }

    /// <summary>
    /// Command for moving an element
    /// </summary>
    public class MoveElementCommand : IUndoableCommand
    {
        private readonly UIElementViewModel _element;
        private readonly double _oldX;
        private readonly double _oldY;
        private readonly double _newX;
        private readonly double _newY;

        public string Description => $"Move {_element.Name}";

        public MoveElementCommand(
            UIElementViewModel element,
            double oldX, double oldY,
            double newX, double newY)
        {
            _element = element;
            _oldX = oldX;
            _oldY = oldY;
            _newX = newX;
            _newY = newY;
        }

        public void Execute()
        {
            _element.X = _newX;
            _element.Y = _newY;
        }

        public void Undo()
        {
            _element.X = _oldX;
            _element.Y = _oldY;
        }
    }

    /// <summary>
    /// Command for resizing an element
    /// </summary>
    public class ResizeElementCommand : IUndoableCommand
    {
        private readonly UIElementViewModel _element;
        private readonly double _oldX;
        private readonly double _oldY;
        private readonly double _oldWidth;
        private readonly double _oldHeight;
        private readonly double _newX;
        private readonly double _newY;
        private readonly double _newWidth;
        private readonly double _newHeight;

        public string Description => $"Resize {_element.Name}";

        public ResizeElementCommand(
            UIElementViewModel element,
            double oldX, double oldY, double oldWidth, double oldHeight,
            double newX, double newY, double newWidth, double newHeight)
        {
            _element = element;
            _oldX = oldX;
            _oldY = oldY;
            _oldWidth = oldWidth;
            _oldHeight = oldHeight;
            _newX = newX;
            _newY = newY;
            _newWidth = newWidth;
            _newHeight = newHeight;
        }

        public void Execute()
        {
            _element.X = _newX;
            _element.Y = _newY;
            _element.Width = _newWidth;
            _element.Height = _newHeight;
        }

        public void Undo()
        {
            _element.X = _oldX;
            _element.Y = _oldY;
            _element.Width = _oldWidth;
            _element.Height = _oldHeight;
        }
    }

    /// <summary>
    /// Command for changing a single property value
    /// </summary>
    public class PropertyChangeCommand : IUndoableCommand
    {
        private readonly UIElementViewModel _element;
        private readonly string _propertyName;
        private readonly object? _oldValue;
        private readonly object? _newValue;
        private readonly Action<UIElementViewModel, string, object?> _setProperty;

        public string Description => $"Change {_propertyName}";

        public PropertyChangeCommand(
            UIElementViewModel element,
            string propertyName,
            object? oldValue,
            object? newValue,
            Action<UIElementViewModel, string, object?> setProperty)
        {
            _element = element;
            _propertyName = propertyName;
            _oldValue = oldValue;
            _newValue = newValue;
            _setProperty = setProperty;
        }

        public void Execute()
        {
            _setProperty(_element, _propertyName, _newValue);
        }

        public void Undo()
        {
            _setProperty(_element, _propertyName, _oldValue);
        }
    }

    /// <summary>
    /// Command for changing multiple properties at once (batch)
    /// </summary>
    public class BatchPropertyChangeCommand : IUndoableCommand
    {
        private readonly UIElementViewModel _element;
        private readonly Dictionary<string, (object? OldValue, object? NewValue)> _changes;
        private readonly Action<UIElementViewModel, string, object?> _setProperty;
        private readonly string _description;

        public string Description => _description;

        public BatchPropertyChangeCommand(
            UIElementViewModel element,
            Dictionary<string, (object? OldValue, object? NewValue)> changes,
            Action<UIElementViewModel, string, object?> setProperty,
            string description = "Change Properties")
        {
            _element = element;
            _changes = new Dictionary<string, (object?, object?)>(changes);
            _setProperty = setProperty;
            _description = description;
        }

        public void Execute()
        {
            foreach (var (propertyName, values) in _changes)
            {
                _setProperty(_element, propertyName, values.NewValue);
            }
        }

        public void Undo()
        {
            foreach (var (propertyName, values) in _changes)
            {
                _setProperty(_element, propertyName, values.OldValue);
            }
        }
    }

    /// <summary>
    /// Command for changing the parent of an element
    /// </summary>
    public class ReparentElementCommand : IUndoableCommand
    {
        private readonly UIElementViewModel _element;
        private readonly UIElementViewModel? _oldParent;
        private readonly UIElementViewModel _newParent;
        private readonly ObservableCollection<UIElementViewModel> _rootElements;
        private readonly Action _renderAll;
        private readonly Func<UIElementViewModel, UIElementViewModel?, UIElementViewModel?, (double X, double Y)> _calculateCoordinates;
        private readonly int _oldIndex;
        private readonly int _newIndex; // -1 means append

        // Store original coordinates for undo
        private readonly double _originalX;
        private readonly double _originalY;

        public string Description => $"Reparent {_element.Name} to {_newParent.Name}";

        public ReparentElementCommand(
            UIElementViewModel element,
            UIElementViewModel newParent,
            ObservableCollection<UIElementViewModel> rootElements,
            Action renderAll,
            Func<UIElementViewModel, UIElementViewModel?, UIElementViewModel?, (double X, double Y)> calculateCoordinates,
            int newIndex = -1)
        {
            _element = element;
            _oldParent = element.Parent;
            _newParent = newParent;
            _newIndex = newIndex;
            _rootElements = rootElements;
            _renderAll = renderAll;
            _calculateCoordinates = calculateCoordinates;

            // Store original coordinates for undo
            _originalX = element.X;
            _originalY = element.Y;

            if (_oldParent != null)
            {
                _oldIndex = _oldParent.Children.IndexOf(_element);
            }
            else
            {
                _oldIndex = _rootElements.IndexOf(_element);
            }
        }

        public void Execute()
        {
            // Unity-style parenting: Maintain world position when parenting
            // CRITICAL: Calculate coordinates FIRST, update hierarchy, THEN set coords, THEN render
            // This avoids PropertyChanged race conditions

            // Step 1: Calculate target coordinates (doesn't trigger PropertyChanged yet)
            var (newX, newY) = _calculateCoordinates(_element, _oldParent, _newParent);

            // Step 2: Update hierarchy (remove from old parent)
            if (_oldParent != null)
            {
                _oldParent.Children.Remove(_element);
            }
            else
            {
                _rootElements.Remove(_element);
            }

            // Step 3: Add to new parent
            if (_newIndex >= 0 && _newIndex <= _newParent.Children.Count)
            {
                _newParent.Children.Insert(_newIndex, _element);
            }
            else
            {
                _newParent.Children.Add(_element);
            }

            // Step 4: Update parent reference
            _element.Parent = _newParent;

            // Step 5: NOW set coordinates (PropertyChanged fires with correct hierarchy)
            _element.X = newX;
            _element.Y = newY;

            // Step 6: Recalculate ZIndex for all siblings in the new parent
            RecalculateSiblingZIndices(_newParent);

            // Step 7: Rebuild all visuals with correct hierarchy and coordinates
            _renderAll();
        }

        /// <summary>
        /// Recalculates ZIndex for all children of a parent based on their position in the Children collection.
        /// This ensures draw order matches hierarchy order.
        /// </summary>
        private static void RecalculateSiblingZIndices(UIElementViewModel parent)
        {
            for (int i = 0; i < parent.Children.Count; i++)
            {
                parent.Children[i].ZIndex = i;
            }
        }

        public void Undo()
        {
            // Undo: Restore original hierarchy and coordinates
            // Same ordering: hierarchy first, then coordinates, then render

            // Step 1: Remove from new parent
            _newParent.Children.Remove(_element);

            // Step 2: Restore to old parent
            if (_oldParent != null)
            {
                if (_oldIndex >= 0 && _oldIndex < _oldParent.Children.Count)
                {
                    _oldParent.Children.Insert(_oldIndex, _element);
                }
                else
                {
                    _oldParent.Children.Add(_element);
                }
                _element.Parent = _oldParent;
            }
            else
            {
                if (_oldIndex >= 0 && _oldIndex < _rootElements.Count)
                {
                    _rootElements.Insert(_oldIndex, _element);
                }
                else
                {
                    _rootElements.Add(_element);
                }
                _element.Parent = null;
            }

            // Step 3: Restore original coordinates (before parenting)
            _element.X = _originalX;
            _element.Y = _originalY;

            // Step 4: Recalculate ZIndex for both old and new parent
            RecalculateSiblingZIndices(_newParent);
            if (_oldParent != null)
            {
                RecalculateSiblingZIndices(_oldParent);
            }

            // Step 5: Rebuild visuals
            _renderAll();
        }
    }

    /// <summary>
    /// Command for changing Z-order (Bring to Front / Send to Back)
    /// </summary>
    public class ZOrderCommand : IUndoableCommand
    {
        private readonly List<UIElementViewModel> _elements;
        private readonly ObservableCollection<UIElementViewModel> _rootElements;
        private readonly bool _bringToFront; // true = front, false = back
        private readonly Action<UIElementViewModel> _updateVisualZOrder;
        private readonly Dictionary<UIElementViewModel, int> _originalIndices = new();
        private readonly Dictionary<UIElementViewModel, UIElementViewModel?> _parents = new();

        public string Description => _bringToFront ? "Bring to Front" : "Send to Back";

        public ZOrderCommand(
            List<UIElementViewModel> elements,
            ObservableCollection<UIElementViewModel> rootElements,
            bool bringToFront,
            Action<UIElementViewModel> updateVisualZOrder)
        {
            _elements = elements.ToList();
            _rootElements = rootElements;
            _bringToFront = bringToFront;
            _updateVisualZOrder = updateVisualZOrder;

            // Store original indices for each element
            foreach (var element in _elements)
            {
                var parent = element.Parent;
                _parents[element] = parent;

                if (parent != null)
                {
                    _originalIndices[element] = parent.Children.IndexOf(element);
                }
                else if (_rootElements.Contains(element))
                {
                    _originalIndices[element] = _rootElements.IndexOf(element);
                }
            }
        }

        public void Execute()
        {
            var affectedParents = new HashSet<UIElementViewModel>();

            foreach (var element in _elements)
            {
                var parent = _parents[element];
                if (parent != null)
                {
                    parent.Children.Remove(element);
                    if (_bringToFront)
                        parent.Children.Add(element);
                    else
                        parent.Children.Insert(0, element);
                    affectedParents.Add(parent);
                }
                else if (_rootElements.Contains(element))
                {
                    _rootElements.Remove(element);
                    if (_bringToFront)
                        _rootElements.Add(element);
                    else
                        _rootElements.Insert(0, element);
                }

                _updateVisualZOrder(element);
            }

            // Recalculate ZIndex for all affected parents
            foreach (var parent in affectedParents)
            {
                RecalculateSiblingZIndices(parent);
            }
        }

        public void Undo()
        {
            var affectedParents = new HashSet<UIElementViewModel>();

            // Restore in reverse order to maintain correct indices
            foreach (var element in _elements.AsEnumerable().Reverse())
            {
                var parent = _parents[element];
                var originalIndex = _originalIndices[element];

                if (parent != null)
                {
                    parent.Children.Remove(element);
                    if (originalIndex >= 0 && originalIndex <= parent.Children.Count)
                        parent.Children.Insert(originalIndex, element);
                    else
                        parent.Children.Add(element);
                    affectedParents.Add(parent);
                }
                else
                {
                    _rootElements.Remove(element);
                    if (originalIndex >= 0 && originalIndex <= _rootElements.Count)
                        _rootElements.Insert(originalIndex, element);
                    else
                        _rootElements.Add(element);
                }

                _updateVisualZOrder(element);
            }

            // Recalculate ZIndex for all affected parents
            foreach (var parent in affectedParents)
            {
                RecalculateSiblingZIndices(parent);
            }
        }

        private static void RecalculateSiblingZIndices(UIElementViewModel parent)
        {
            for (int i = 0; i < parent.Children.Count; i++)
            {
                parent.Children[i].ZIndex = i;
            }
        }
    }

    /// <summary>
    /// Command for batch moving multiple elements (alignment operations, multi-select drag)
    /// </summary>
    public class BatchMoveCommand : IUndoableCommand
    {
        private readonly List<(UIElementViewModel Element, double OldX, double OldY, double NewX, double NewY)> _moves;
        private readonly string _description;

        public string Description => _description;

        public BatchMoveCommand(
            List<(UIElementViewModel Element, double OldX, double OldY, double NewX, double NewY)> moves,
            string description = "Move Elements")
        {
            _moves = moves.ToList();
            _description = description;
        }

        public void Execute()
        {
            foreach (var (element, _, _, newX, newY) in _moves)
            {
                element.X = newX;
                element.Y = newY;
            }
        }

        public void Undo()
        {
            foreach (var (element, oldX, oldY, _, _) in _moves)
            {
                element.X = oldX;
                element.Y = oldY;
            }
        }
    }

    /// <summary>
    /// Command for batch resizing multiple elements
    /// </summary>
    public class BatchResizeCommand : IUndoableCommand
    {
        private readonly List<(UIElementViewModel Element, double OldX, double OldY, double OldWidth, double OldHeight, double NewX, double NewY, double NewWidth, double NewHeight)> _resizes;
        private readonly string _description;

        public string Description => _description;

        public BatchResizeCommand(
            List<(UIElementViewModel Element, double OldX, double OldY, double OldWidth, double OldHeight, double NewX, double NewY, double NewWidth, double NewHeight)> resizes,
            string description = "Resize Elements")
        {
            _resizes = resizes.ToList();
            _description = description;
        }

        public void Execute()
        {
            foreach (var (element, _, _, _, _, newX, newY, newWidth, newHeight) in _resizes)
            {
                element.X = newX;
                element.Y = newY;
                element.Width = newWidth;
                element.Height = newHeight;
            }
        }

        public void Undo()
        {
            foreach (var (element, oldX, oldY, oldWidth, oldHeight, _, _, _, _) in _resizes)
            {
                element.X = oldX;
                element.Y = oldY;
                element.Width = oldWidth;
                element.Height = oldHeight;
            }
        }
    }

    /// <summary>
    /// Command for grouping elements under a new parent (Create Parent operation)
    /// </summary>
    public class GroupElementsCommand : IUndoableCommand
    {
        private readonly List<UIElementViewModel> _elements;
        private readonly UIElementViewModel _newParent;
        private readonly UIElementViewModel? _commonParent;
        private readonly ObservableCollection<UIElementViewModel> _rootElements;
        private readonly Action _renderAll;
        private readonly Action<UIElementViewModel> _selectElement;

        // Store original state for undo
        private readonly Dictionary<UIElementViewModel, UIElementViewModel?> _originalParents = new();
        private readonly Dictionary<UIElementViewModel, int> _originalIndices = new();
        private readonly Dictionary<UIElementViewModel, (double X, double Y)> _originalPositions = new();
        private readonly Dictionary<UIElementViewModel, (double X, double Y)> _newPositions = new();
        private readonly double _minX;
        private readonly double _minY;
        private int _newParentIndex;

        public string Description => "Group Elements";

        public GroupElementsCommand(
            List<UIElementViewModel> elements,
            UIElementViewModel newParent,
            UIElementViewModel? commonParent,
            ObservableCollection<UIElementViewModel> rootElements,
            Action renderAll,
            Action<UIElementViewModel> selectElement,
            double minX,
            double minY)
        {
            _elements = elements.ToList();
            _newParent = newParent;
            _commonParent = commonParent;
            _rootElements = rootElements;
            _renderAll = renderAll;
            _selectElement = selectElement;
            _minX = minX;
            _minY = minY;

            // Store original state
            foreach (var element in _elements)
            {
                _originalParents[element] = element.Parent;
                _originalPositions[element] = (element.X, element.Y);
                _newPositions[element] = (element.X - minX, element.Y - minY);

                if (element.Parent != null)
                {
                    _originalIndices[element] = element.Parent.Children.IndexOf(element);
                }
                else
                {
                    _originalIndices[element] = _rootElements.IndexOf(element);
                }
            }
        }

        public void Execute()
        {
            // Add new parent to hierarchy
            if (_commonParent != null)
            {
                _commonParent.Children.Add(_newParent);
                _newParent.Parent = _commonParent;
                _newParent.ZIndex = _commonParent.Children.Count - 1;
                _newParentIndex = _commonParent.Children.Count - 1;
            }
            else
            {
                _rootElements.Add(_newParent);
                _newParent.ZIndex = _rootElements.Count - 1;
                _newParentIndex = _rootElements.Count - 1;
            }

            // Get elements sorted by original order
            var sortedElements = _elements
                .OrderBy(el => _originalIndices[el])
                .ToList();

            // Reparent all elements to new parent
            foreach (var element in sortedElements)
            {
                // Remove from old parent
                if (element.Parent != null)
                {
                    element.Parent.Children.Remove(element);
                }
                else
                {
                    _rootElements.Remove(element);
                }

                // Adjust position to be relative to new parent
                var (newX, newY) = _newPositions[element];
                element.X = newX;
                element.Y = newY;

                // Add to new parent
                _newParent.Children.Add(element);
                element.Parent = _newParent;
            }

            // Set ZIndex for all children
            for (int i = 0; i < _newParent.Children.Count; i++)
            {
                _newParent.Children[i].ZIndex = i;
            }

            _renderAll();
            _selectElement(_newParent);
        }

        public void Undo()
        {
            // Restore all elements to their original parents in reverse order
            var sortedElements = _elements
                .OrderByDescending(el => _originalIndices[el])
                .ToList();

            foreach (var element in sortedElements)
            {
                // Remove from new parent
                _newParent.Children.Remove(element);

                // Restore original position
                var (origX, origY) = _originalPositions[element];
                element.X = origX;
                element.Y = origY;

                // Restore to original parent
                var originalParent = _originalParents[element];
                var originalIndex = _originalIndices[element];

                if (originalParent != null)
                {
                    if (originalIndex >= 0 && originalIndex <= originalParent.Children.Count)
                        originalParent.Children.Insert(originalIndex, element);
                    else
                        originalParent.Children.Add(element);
                    element.Parent = originalParent;
                }
                else
                {
                    if (originalIndex >= 0 && originalIndex <= _rootElements.Count)
                        _rootElements.Insert(originalIndex, element);
                    else
                        _rootElements.Add(element);
                    element.Parent = null;
                }
            }

            // Remove the new parent
            if (_commonParent != null)
            {
                _commonParent.Children.Remove(_newParent);
            }
            else
            {
                _rootElements.Remove(_newParent);
            }
            _newParent.Parent = null;

            _renderAll();
        }
    }

    /// <summary>
    /// Composite command that combines multiple commands into a single undoable operation
    /// </summary>
    public class CompositeCommand : IUndoableCommand
    {
        private readonly List<IUndoableCommand> _commands;
        private readonly string _description;

        public string Description => _description;

        public CompositeCommand(List<IUndoableCommand> commands, string description = "Multiple Changes")
        {
            _commands = commands.ToList();
            _description = description;
        }

        public void Execute()
        {
            foreach (var command in _commands)
            {
                command.Execute();
            }
        }

        public void Undo()
        {
            // Undo in reverse order
            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                _commands[i].Undo();
            }
        }
    }
}

// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections.ObjectModel;
using HS.Stride.UI.Editor.Core.Models;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Core.Services
{
    /// <summary>
    /// Manages the core editor state including selection, hierarchy, and element management
    /// </summary>
    public class EditorStateService
    {
        private readonly Dictionary<string, int> _elementCounters = new();

        public EditorState EditorState { get; }
        public SelectionState SelectionState { get; }
        public ObservableCollection<UIElementViewModel> RootElements { get; }

        public event EventHandler? SelectionChanged;
        public event EventHandler<UIElementViewModel>? ElementAdded;
        public event EventHandler<UIElementViewModel>? ElementRemoved;

        public EditorStateService()
        {
            EditorState = new EditorState();
            SelectionState = new SelectionState();
            RootElements = new ObservableCollection<UIElementViewModel>();
        }

        #region Selection Management

        /// <summary>
        /// Select a single element, optionally adding to existing selection
        /// </summary>
        public void SelectElement(UIElementViewModel element, bool addToSelection = false)
        {
            if (addToSelection)
            {
                // Ctrl+Click: Toggle selection
                if (SelectionState.Contains(element))
                {
                    SelectionState.Remove(element);
                }
                else
                {
                    SelectionState.Add(element);
                }
            }
            else
            {
                // Normal click: Clear and select only this
                SelectionState.Set(element);
            }

            OnSelectionChanged();
        }

        /// <summary>
        /// Clear all selection
        /// </summary>
        public void ClearSelection()
        {
            if (SelectionState.HasSelection)
            {
                SelectionState.Clear();
                OnSelectionChanged();
            }
        }

        /// <summary>
        /// Get the primary selected element (first in selection)
        /// </summary>
        public UIElementViewModel? GetPrimarySelection()
        {
            return SelectionState.PrimarySelection;
        }

        /// <summary>
        /// Get all selected elements
        /// </summary>
        public IReadOnlyList<UIElementViewModel> GetSelectedElements()
        {
            return SelectionState.SelectedElements.AsReadOnly();
        }

        #endregion

        #region Element Hierarchy Management

        /// <summary>
        /// Add an element to the hierarchy
        /// </summary>
        public void AddElement(UIElementViewModel element, UIElementViewModel? parent = null)
        {
            if (parent == null)
            {
                // Add to root
                RootElements.Add(element);
            }
            else
            {
                // Add to parent's children
                parent.Children.Add(element);
                element.Parent = parent;
            }

            ElementAdded?.Invoke(this, element);
        }

        /// <summary>
        /// Remove an element from the hierarchy
        /// </summary>
        public void RemoveElement(UIElementViewModel element)
        {
            // Remove from selection first
            if (SelectionState.Contains(element))
            {
                SelectionState.Remove(element);
                OnSelectionChanged();
            }

            // Remove from hierarchy
            if (element.Parent != null)
            {
                element.Parent.Children.Remove(element);
                element.Parent = null;
            }
            else
            {
                RootElements.Remove(element);
            }

            ElementRemoved?.Invoke(this, element);
        }

        /// <summary>
        /// Find an element by ID in the entire hierarchy
        /// </summary>
        public UIElementViewModel? FindElementById(string id)
        {
            foreach (var root in RootElements)
            {
                var found = FindElementByIdRecursive(root, id);
                if (found != null)
                    return found;
            }
            return null;
        }

        private UIElementViewModel? FindElementByIdRecursive(UIElementViewModel element, string id)
        {
            if (element.Id == id)
                return element;

            foreach (var child in element.Children)
            {
                var found = FindElementByIdRecursive(child, id);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// Get all elements in the hierarchy (flattened)
        /// </summary>
        public List<UIElementViewModel> GetAllElements()
        {
            var allElements = new List<UIElementViewModel>();
            foreach (var root in RootElements)
            {
                CollectElementsRecursive(root, allElements);
            }
            return allElements;
        }

        private void CollectElementsRecursive(UIElementViewModel element, List<UIElementViewModel> collection)
        {
            collection.Add(element);
            foreach (var child in element.Children)
            {
                CollectElementsRecursive(child, collection);
            }
        }

        /// <summary>
        /// Get visible hierarchy (children of root Grid if it's a system element, otherwise root elements)
        /// </summary>
        public ObservableCollection<UIElementViewModel> GetVisibleHierarchy()
        {
            if (RootElements.Count > 0 && RootElements[0].IsSystemElement)
            {
                return RootElements[0].Children;
            }
            return RootElements;
        }

        #endregion

        #region Element Naming

        /// <summary>
        /// Generate a clean, sequential name for a UI element
        /// </summary>
        public string GenerateElementName(string elementType)
        {
            if (!_elementCounters.ContainsKey(elementType))
            {
                _elementCounters[elementType] = 1;
            }
            else
            {
                _elementCounters[elementType]++;
            }

            return $"{elementType}{_elementCounters[elementType]}";
        }

        /// <summary>
        /// Reset element naming counters (e.g., when starting a new document)
        /// </summary>
        public void ResetElementCounters()
        {
            _elementCounters.Clear();
        }

        #endregion

        #region State Management

        /// <summary>
        /// Clear all editor state (new document)
        /// </summary>
        public void ClearAll()
        {
            ClearSelection();
            RootElements.Clear();
            ResetElementCounters();
        }

        #endregion

        private void OnSelectionChanged()
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

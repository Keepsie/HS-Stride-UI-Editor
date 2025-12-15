// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using HS.Stride.UI.Editor.Core.Services.Interfaces;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Core.Services
{
    /// <summary>
    /// Manages element selection state
    /// </summary>
    public class SelectionService : ISelectionService
    {
        private readonly List<UIElementViewModel> _selectedElements = new();

        public IReadOnlyList<UIElementViewModel> SelectedElements => _selectedElements.AsReadOnly();

        public UIElementViewModel? PrimarySelection =>
            _selectedElements.Count > 0 ? _selectedElements[0] : null;

        public void Select(UIElementViewModel element, bool addToSelection = false)
        {
            if (addToSelection)
            {
                // Ctrl+Click: Toggle selection
                if (_selectedElements.Contains(element))
                {
                    // Remove from selection
                    element.IsSelected = false;
                    _selectedElements.Remove(element);
                }
                else
                {
                    // Add to selection
                    element.IsSelected = true;
                    _selectedElements.Add(element);
                }
            }
            else
            {
                // Normal click: Clear and select only this
                ClearSelection();
                element.IsSelected = true;
                _selectedElements.Add(element);
            }
        }

        public void ClearSelection()
        {
            foreach (var element in _selectedElements)
            {
                element.IsSelected = false;
            }
            _selectedElements.Clear();
        }

        public bool IsSelected(UIElementViewModel element)
        {
            return _selectedElements.Contains(element);
        }
    }
}

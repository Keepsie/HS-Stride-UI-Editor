// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Core.Models
{
    /// <summary>
    /// Encapsulates the current selection state in the editor
    /// </summary>
    public class SelectionState
    {
        public List<UIElementViewModel> SelectedElements { get; } = new();

        public UIElementViewModel? PrimarySelection => SelectedElements.Count > 0 ? SelectedElements[0] : null;

        public bool HasSelection => SelectedElements.Count > 0;

        public bool HasMultipleSelection => SelectedElements.Count > 1;

        public int SelectionCount => SelectedElements.Count;

        public void Clear()
        {
            foreach (var element in SelectedElements)
            {
                element.IsSelected = false;
            }
            SelectedElements.Clear();
        }

        public void Add(UIElementViewModel element)
        {
            if (!SelectedElements.Contains(element))
            {
                element.IsSelected = true;
                SelectedElements.Add(element);
            }
        }

        public void Remove(UIElementViewModel element)
        {
            if (SelectedElements.Contains(element))
            {
                element.IsSelected = false;
                SelectedElements.Remove(element);
            }
        }

        public void Set(UIElementViewModel element)
        {
            Clear();
            Add(element);
        }

        public bool Contains(UIElementViewModel element)
        {
            return SelectedElements.Contains(element);
        }
    }
}

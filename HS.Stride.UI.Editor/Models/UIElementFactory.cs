// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace HS.Stride.UI.Editor.Models
{
    /// <summary>
    /// Represents a factory for creating UI elements in the editor
    /// </summary>
    public class UIElementFactory
    {
        public string Name { get; set; } = string.Empty;
        public string ElementType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public UIElementFactory(string name, string elementType, string category, string description = "")
        {
            Name = name;
            ElementType = elementType;
            Category = category;
            Description = description;
        }
    }
}

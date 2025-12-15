// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows.Media;

namespace HS.Stride.UI.Editor.Controls.Elements.Containers
{
    /// <summary>
    /// Handler for Canvas container elements
    /// </summary>
    public class CanvasHandler : ElementHandlerBase
    {
        public override string ElementType => "Canvas";
        public override double DefaultWidth => 200;
        public override double DefaultHeight => 200;

        public override Brush GetHintBrush()
        {
            return new SolidColorBrush(Color.FromArgb(30, 100, 150, 255)); // Blue tint for containers
        }

        public override IEnumerable<string> GetPropertyCategories()
        {
            foreach (var cat in base.GetPropertyCategories())
                yield return cat;
            // Canvas has no additional properties beyond base
        }
    }
}

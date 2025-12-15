// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows.Media;

namespace HS.Stride.UI.Editor.Controls.Elements.Visual
{
    /// <summary>
    /// Handler for ContentControl visual elements
    /// </summary>
    public class ContentControlHandler : ElementHandlerBase
    {
        public override string ElementType => "ContentControl";
        public override double DefaultWidth => 100;
        public override double DefaultHeight => 100;

        public override Brush GetHintBrush()
        {
            return new SolidColorBrush(Color.FromArgb(30, 180, 80, 180));
        }

        public override IEnumerable<string> GetPropertyCategories()
        {
            foreach (var cat in base.GetPropertyCategories())
                yield return cat;
            // ContentControl has no additional properties beyond base
        }
    }
}

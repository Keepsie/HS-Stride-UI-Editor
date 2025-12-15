// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows.Media;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor.Controls.Elements.Visual
{
    /// <summary>
    /// Handler for Modal overlay visual elements
    /// </summary>
    public class ModalHandler : ElementHandlerBase
    {
        public override string ElementType => "Modal";
        public override double DefaultWidth => 300;
        public override double DefaultHeight => 200;
        public override Color DefaultBackgroundColor => Color.FromArgb(180, 0, 0, 0);

        public override Brush GetFillBrush(UIElementViewModel viewModel)
        {
            if (viewModel.BackgroundColor != Colors.Transparent)
                return new SolidColorBrush(viewModel.BackgroundColor);

            return new SolidColorBrush(DefaultBackgroundColor);
        }

        public override Brush GetHintBrush()
        {
            return new SolidColorBrush(Color.FromArgb(30, 180, 80, 180));
        }

        public override IEnumerable<string> GetPropertyCategories()
        {
            foreach (var cat in base.GetPropertyCategories())
                yield return cat;
            yield return "Modal";
        }
    }
}

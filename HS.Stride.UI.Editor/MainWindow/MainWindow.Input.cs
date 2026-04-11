// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace HS.Stride.UI.Editor
{
    /// <summary>
    /// MainWindow partial class - Keyboard input handling
    /// </summary>
    public partial class MainWindow
    {
        private static DependencyObject? GetParentObject(DependencyObject source)
        {
            if (source is Visual || source is Visual3D)
                return VisualTreeHelper.GetParent(source);

            return LogicalTreeHelper.GetParent(source);
        }

        private static bool IsTypeOrAncestor<T>(DependencyObject? source) where T : DependencyObject
        {
            while (source != null)
            {
                if (source is T)
                    return true;

                source = GetParentObject(source);
            }

            return false;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;

            // If focus is in text-editing controls, never intercept global shortcuts.
            if (IsTypeOrAncestor<TextBoxBase>(source) || IsTypeOrAncestor<PasswordBox>(source))
                return;

            // Arrow keys should navigate UI controls (hierarchy/content/property selectors),
            // not move elements while those controls are focused.
            bool isArrowKey = e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down;
            if (isArrowKey &&
                (IsTypeOrAncestor<TreeView>(source) ||
                 IsTypeOrAncestor<Selector>(source) ||
                 IsTypeOrAncestor<Slider>(source)))
            {
                return;
            }

            bool handled = true;

            // Check for modifiers
            bool ctrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            bool shiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;

            switch (e.Key)
            {
                case Key.Delete:
                    DeleteElement_Click(this, new RoutedEventArgs());
                    break;

                case Key.D when ctrlPressed:
                    DuplicateElement_Click(this, new RoutedEventArgs());
                    break;

                case Key.X when ctrlPressed:
                    CutElement();
                    break;

                case Key.C when ctrlPressed:
                    CopyElement();
                    break;

                case Key.V when ctrlPressed:
                    PasteElement();
                    break;

                case Key.Z when ctrlPressed && shiftPressed:
                    Redo();
                    break;

                case Key.Z when ctrlPressed:
                    Undo();
                    break;

                case Key.Y when ctrlPressed:
                    Redo();
                    break;

                case Key.S when ctrlPressed && shiftPressed:
                    if (_isDocumentLoaded)
                        MenuSaveAs_Click(this, new RoutedEventArgs());
                    break;

                case Key.S when ctrlPressed:
                    if (_isDocumentLoaded)
                        MenuSave_Click(this, new RoutedEventArgs());
                    break;

                case Key.Left:
                    NudgeElement(-1 * (shiftPressed ? 10 : 1), 0);
                    break;

                case Key.Right:
                    NudgeElement(1 * (shiftPressed ? 10 : 1), 0);
                    break;

                case Key.Up:
                    NudgeElement(0, -1 * (shiftPressed ? 10 : 1));
                    break;

                case Key.Down:
                    NudgeElement(0, 1 * (shiftPressed ? 10 : 1));
                    break;

                case Key.Space:
                    // Space for pan mode (Photoshop style)
                    if (!_isSpaceHeld)
                    {
                        _isSpaceHeld = true;
                        CanvasScrollViewer.Cursor = Cursors.ScrollAll;
                    }
                    handled = true;
                    break;

                default:
                    handled = false;
                    break;
            }

            e.Handled = handled;
        }

        private void MainWindow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                _isSpaceHeld = false;
                if (!_isPanning)
                {
                    CanvasScrollViewer.Cursor = Cursors.Arrow;
                }
            }
        }
    }
}

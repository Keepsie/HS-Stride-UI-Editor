// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Windows;

namespace HS.Stride.UI.Editor.Views
{
    public partial class KeyboardShortcutsDialog : Window
    {
        public KeyboardShortcutsDialog()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

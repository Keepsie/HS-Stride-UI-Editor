// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using HS.Stride.UI.Editor.Models;

namespace HS.Stride.UI.Editor.Views
{
    public partial class AssetPickerDialog : Window
    {
        public AssetItem? SelectedAsset { get; private set; }
        private ObservableCollection<AssetItem> _allAssets = new();

        public AssetPickerDialog(List<AssetItem> assets)
        {
            InitializeComponent();

            _allAssets = new ObservableCollection<AssetItem>(assets);
            AssetListView.ItemsSource = _allAssets;
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Search assets...")
            {
                SearchBox.Text = "";
                SearchBox.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = "Search assets...";
                SearchBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Guard against event firing before initialization complete
            if (_allAssets == null || AssetListView == null) return;

            var searchText = SearchBox.Text;

            // Ignore placeholder text
            if (searchText == "Search assets..." || string.IsNullOrWhiteSpace(searchText))
            {
                AssetListView.ItemsSource = _allAssets;
                return;
            }

            // Filter assets by name (case-insensitive)
            var filtered = _allAssets.Where(a =>
                (a.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.Path?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            AssetListView.ItemsSource = filtered;
        }

        private void AssetListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AssetListView.SelectedItem is AssetItem asset)
            {
                SelectedAsset = asset;
                DialogResult = true;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (AssetListView.SelectedItem is AssetItem asset)
            {
                SelectedAsset = asset;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please select an asset.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.IO;
using System.Windows;
using Microsoft.Win32;
using HS.Stride.Mass.Importer.Core;

namespace HS.Stride.UI.Editor.Views
{
    /// <summary>
    /// Dialog for importing assets (textures or fonts) into a Stride project
    /// </summary>
    public partial class ImportAssetsDialog : Window
    {
        private readonly StrideMassImporter _importer;
        private readonly string _strideProjectPath;
        private readonly ImportType _importType;
        private readonly List<FileItem> _selectedFiles = new();

        public enum ImportType
        {
            Textures,
            Fonts
        }

        public class FileItem
        {
            public string FullPath { get; set; } = "";
            public string FileName => Path.GetFileName(FullPath);
            public string FolderPath => Path.GetDirectoryName(FullPath) ?? "";
        }

        public ImportAssetsDialog(string strideProjectPath, ImportType importType)
        {
            InitializeComponent();

            _importer = new StrideMassImporter();
            _strideProjectPath = strideProjectPath;
            _importType = importType;

            // Configure dialog based on import type
            if (importType == ImportType.Textures)
            {
                TitleText.Text = "Import Textures";
                SubtitleText.Text = "Select image files to import as Stride texture assets";
                PackageNameTextBox.Text = "Textures";
            }
            else
            {
                TitleText.Text = "Import Fonts";
                SubtitleText.Text = "Select font files to import as Stride sprite font assets";
                PackageNameTextBox.Text = "Fonts";
            }

            UpdateUI();
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = _importType == ImportType.Textures ? "Select Images" : "Select Fonts"
            };

            if (_importType == ImportType.Textures)
            {
                dialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.tga;*.dds|PNG Files|*.png|JPEG Files|*.jpg;*.jpeg|All Files|*.*";
            }
            else
            {
                dialog.Filter = "Font Files|*.ttf;*.otf;*.ttc;*.woff;*.woff2|TrueType Fonts|*.ttf|OpenType Fonts|*.otf|All Files|*.*";
            }

            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    // Avoid duplicates
                    if (!_selectedFiles.Any(f => f.FullPath.Equals(file, StringComparison.OrdinalIgnoreCase)))
                    {
                        _selectedFiles.Add(new FileItem { FullPath = file });
                    }
                }

                UpdateUI();
            }
        }

        private void ClearFiles_Click(object sender, RoutedEventArgs e)
        {
            _selectedFiles.Clear();
            UpdateUI();
        }

        private void UpdateUI()
        {
            FileListBox.ItemsSource = null;
            FileListBox.ItemsSource = _selectedFiles;
            FileCountText.Text = $"({_selectedFiles.Count} file{(_selectedFiles.Count != 1 ? "s" : "")})";
            ImportButton.IsEnabled = _selectedFiles.Count > 0 && !string.IsNullOrWhiteSpace(PackageNameTextBox.Text);

            if (_selectedFiles.Count == 0)
            {
                StatusText.Text = "Select files to import";
            }
            else
            {
                var assetType = _importType == ImportType.Textures ? "texture" : "font";
                StatusText.Text = $"Ready to import {_selectedFiles.Count} {assetType}{(_selectedFiles.Count != 1 ? "s" : "")} to project";
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var packageName = PackageNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(packageName))
            {
                MessageBox.Show("Please enter a package name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedFiles.Count == 0)
            {
                MessageBox.Show("Please select files to import.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Disable UI during import
            ImportButton.IsEnabled = false;
            StatusText.Text = "Importing...";

            try
            {
                // Create a temp folder with just the selected files
                var tempFolder = Path.Combine(Path.GetTempPath(), $"HS_Import_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempFolder);

                try
                {
                    // Copy selected files to temp folder
                    foreach (var file in _selectedFiles)
                    {
                        var destPath = Path.Combine(tempFolder, file.FileName);
                        File.Copy(file.FullPath, destPath, overwrite: true);
                    }

                    // Run the import
                    var importResult = await _importer.ImportAssetsAsync(packageName, tempFolder, _strideProjectPath, createMaterials: false);

                    if (importResult.Success)
                    {
                        var assetType = _importType == ImportType.Textures ? "texture" : "font";
                        MessageBox.Show(
                            $"Successfully imported {importResult.AssetsCreated} {assetType}{(importResult.AssetsCreated != 1 ? "s" : "")}!\n\n" +
                            $"Assets: {importResult.TargetAssetsPath}\n" +
                            $"Resources: {importResult.TargetResourcesPath}",
                            "Import Complete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Import failed: {importResult.ErrorMessage}",
                            "Import Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                finally
                {
                    // Cleanup temp folder
                    try
                    {
                        Directory.Delete(tempFolder, recursive: true);
                    }
                    catch { /* ignore cleanup errors */ }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Import failed: {ex.Message}",
                    "Import Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                ImportButton.IsEnabled = true;
                UpdateUI();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

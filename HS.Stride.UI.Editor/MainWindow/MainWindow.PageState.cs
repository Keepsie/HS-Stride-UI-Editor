// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.IO;
using System.Windows;
using HS.Stride.UI.Editor.Core.Models;
using HS.Stride.UI.Editor.Core.Services;
using HS.Stride.UI.Editor.ViewModels;

namespace HS.Stride.UI.Editor
{
    /// <summary>
    /// MainWindow partial class - Page editor state persistence (guides, reference images, element states)
    /// </summary>
    public partial class MainWindow
    {

        /// <summary>
        /// Saves the current page's editor state (guides, reference image, etc.)
        /// </summary>
        private void SaveCurrentPageState()
        {
            var projectPath = _connectedProjectSlnPath;
            var pagePath = _fileService.CurrentFilePath;

            if (string.IsNullOrEmpty(projectPath) || string.IsNullOrEmpty(pagePath))
                return;

            var pageState = new PageEditorState
            {
                PagePath = pagePath,
                LastOpened = DateTime.Now,
                ReferenceImagePath = _currentReferenceImagePath,
                ReferenceImageOpacity = RefImageOpacitySlider.Value / 100.0
            };

            // Save guides
            foreach (var guide in _guides)
            {
                pageState.Guides.Add(new GuideData
                {
                    IsHorizontal = guide.IsHorizontal,
                    Position = guide.Position
                });
            }

            // Save element editor states (IsLocked, AllowCanvasOverflow)
            SaveElementEditorStates(pageState, RootElements);

            _editorDataService.SavePageState(projectPath, pageState);
        }

        /// <summary>
        /// Recursively saves editor-only element properties to the page state
        /// </summary>
        private void SaveElementEditorStates(PageEditorState pageState, IEnumerable<UIElementViewModel> elements)
        {
            foreach (var element in elements)
            {
                // Only save if element has non-default editor state
                if (element.IsLocked || element.AllowCanvasOverflow)
                {
                    pageState.Elements[element.Name] = new ElementEditorState
                    {
                        IsLocked = element.IsLocked,
                        AllowCanvasOverflow = element.AllowCanvasOverflow
                    };
                }

                // Recurse into children
                SaveElementEditorStates(pageState, element.Children);
            }
        }

        /// <summary>
        /// Restores page editor state (guides, reference image, etc.) for the current page
        /// </summary>
        private void RestorePageState()
        {
            var projectPath = _connectedProjectSlnPath;
            var pagePath = _fileService.CurrentFilePath;

            if (string.IsNullOrEmpty(projectPath) || string.IsNullOrEmpty(pagePath))
                return;

            var pageState = _editorDataService.LoadPageState(projectPath, pagePath);
            if (pageState == null)
                return;

            // Clear existing guides
            ClearAllGuides();

            // Restore guides
            foreach (var guideData in pageState.Guides)
            {
                CreateGuide(guideData.IsHorizontal, guideData.Position);
            }

            // Restore reference image
            if (!string.IsNullOrEmpty(pageState.ReferenceImagePath) && File.Exists(pageState.ReferenceImagePath))
            {
                LoadReferenceImage(pageState.ReferenceImagePath, pageState.ReferenceImageOpacity);
            }

            // Restore element editor states (IsLocked, AllowCanvasOverflow)
            RestoreElementEditorStates(pageState, RootElements);
        }

        /// <summary>
        /// Recursively restores editor-only element properties from the page state
        /// </summary>
        private void RestoreElementEditorStates(PageEditorState pageState, IEnumerable<UIElementViewModel> elements)
        {
            foreach (var element in elements)
            {
                // Look up editor state by element name
                if (pageState.Elements.TryGetValue(element.Name, out var editorState))
                {
                    element.IsLocked = editorState.IsLocked;
                    element.AllowCanvasOverflow = editorState.AllowCanvasOverflow;
                }

                // Recurse into children
                RestoreElementEditorStates(pageState, element.Children);
            }
        }

        /// <summary>
        /// Clears all guides from the canvas
        /// </summary>
        private void ClearAllGuides()
        {
            foreach (var guide in _guides.ToList())
            {
                GuidesCanvas.Children.Remove(guide);
            }
            _guides.Clear();
        }

        /// <summary>
        /// Clears all page-specific editor state (guides, reference image)
        /// </summary>
        private void ClearPageEditorState()
        {
            ClearAllGuides();
            ReferenceImage.Source = null;
            ReferenceImage.Visibility = Visibility.Collapsed;
            MenuClearRefImage.IsEnabled = false;
            _currentReferenceImagePath = null;
        }

        /// <summary>
        /// Loads a reference image with specified opacity
        /// </summary>
        private void LoadReferenceImage(string imagePath, double opacity)
        {
            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                ReferenceImage.Source = bitmap;
                ReferenceImage.Visibility = Visibility.Visible;
                ReferenceImage.Opacity = opacity;
                RefImageOpacitySlider.Value = opacity * 100.0;

                _currentReferenceImagePath = imagePath;
                MenuClearRefImage.IsEnabled = true;
            }
            catch
            {
                // Silently fail if image can't be loaded
            }
        }
    }
}

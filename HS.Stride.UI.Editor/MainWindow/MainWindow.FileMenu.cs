// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HS.Stride.Editor.Toolkit.Core;
using HS.Stride.UI.Editor.Core.Services.Interfaces;
using Microsoft.Win32;

namespace HS.Stride.UI.Editor
{
    /// <summary>
    /// MainWindow partial class - File menu handlers (new, open, save, import, recent)
    /// </summary>
    public partial class MainWindow
    {

        private async void MenuConnectProject_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Stride Solution (*.sln)|*.sln",
                Title = "Select Stride Project Solution"
            };

            if (dialog.ShowDialog() == true)
            {
                await ConnectToProjectAsync(dialog.FileName);
            }
        }

        private async Task ConnectToProjectAsync(string slnFilePath)
        {
            try
            {
                // Get project folder from .sln file
                var projectFolder = Path.GetDirectoryName(slnFilePath);
                if (string.IsNullOrEmpty(projectFolder))
                {
                    MessageBox.Show("Invalid project path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StrideProject? project = null;

                // Load project on background thread with loading overlay
                await RunWithLoadingAsync("Connecting to project...", async () =>
                {
                    project = await Task.Run(() => new StrideProject(projectFolder));
                });

                if (project == null) return;

                _connectedProject = project;
                _connectedProjectSlnPath = slnFilePath; // Store .sln path for recent projects

                // Set project in services
                _fileService.SetProject(_connectedProject);
                _assetService.ConnectToProject(projectFolder);
                _fileService.SetAssetService(_assetService);

                // Wire up PropertyPanel to use project fonts
                PropertiesPanel.SetAssetService(_assetService);

                // Enable file menu items
                MenuNewUIPage.IsEnabled = true;
                MenuOpen.IsEnabled = true;
                MenuImportTextures.IsEnabled = true;
                MenuImportFonts.IsEnabled = true;

                // Populate project content and restore folder filter
                PopulateProjectContent();
                RestoreContentBrowserState();

                // Auto-create a new page to save time
                MenuNewUIPage_Click(null, null!);

                // Add to recent projects and update menus
                var projectData = _editorDataService.GetOrCreateProjectData(slnFilePath);
                _editorDataService.SaveProjectData(projectData);
                UpdateRecentProjectsMenu();
                UpdateRecentPagesMenu();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to project:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuNewUIPage_Click(object? sender, RoutedEventArgs e)
        {
            if (_connectedProject == null) return;

            // Check for unsaved changes before creating new document
            if (!CheckUnsavedChangesAndPrompt()) return;

            try
            {
                // Clear selection first
                ClearSelection();

                // Clear page-specific editor state (guides, reference image)
                ClearPageEditorState();

                // Reset element naming counters for new document
                _elementCounters.Clear();

                // Create new blank UI page via file service
                var rootElements = _fileService.CreateNewDocument(_designWidth, _designHeight);

                RootElements.Clear();
                foreach (var element in rootElements)
                {
                    RootElements.Add(element);
                }

                // IMPORTANT: Rebind TreeView to new hierarchy (VisibleHierarchy returns different collection now)
                VisualTreeView.ItemsSource = VisibleHierarchy;

                // Clear canvas and re-render
                RenderAllElements();

                // Document is now loaded - allow editing
                _isDocumentLoaded = true;

                MenuSave.IsEnabled = true;  // Will act as Save As for new pages
                MenuSaveAs.IsEnabled = true;
                MenuUnloadPage.IsEnabled = true;
                QuickSaveButton.IsEnabled = true;  // Will act as Save As for new pages

                // Update title using consistent method (no asterisk for fresh new page)
                UpdateTitleWithUnsavedIndicator();
                PageNameIndicator.Text = "New Page";
                PageNameIndicator.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create new UI page:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuUnloadPage_Click(object sender, RoutedEventArgs e)
        {
            if (_connectedProject == null) return;

            // Check for unsaved changes before unloading
            if (!CheckUnsavedChangesAndPrompt()) return;

            // Just create a new blank page (same as New UI Page but semantically "unload")
            MenuNewUIPage_Click(null, null!);
        }

        private async void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            if (_connectedProject == null) return;

            // Check for unsaved changes before opening another document
            if (!CheckUnsavedChangesAndPrompt()) return;

            var dialog = new OpenFileDialog
            {
                Filter = "Stride UI Page (*.sduipage)|*.sduipage",
                Title = "Open UI Page",
                InitialDirectory = _connectedProject.AssetsPath
            };

            if (dialog.ShowDialog() == true)
            {
                await LoadDocumentAsync(dialog.FileName);
            }
        }

        /// <summary>
        /// Load a UI page document asynchronously with loading overlay
        /// </summary>
        private async Task LoadDocumentAsync(string filePath)
        {
            try
            {
                // Clear selection first
                ClearSelection();

                // Clear page-specific editor state (guides, reference image) before loading new page
                ClearPageEditorState();

                // Reset element naming counters for loaded document
                _elementCounters.Clear();

                LoadDocumentResult? loadResult = null;

                // Load via file service on background thread
                await RunWithLoadingAsync("Loading page...", async () =>
                {
                    loadResult = await Task.Run(() => _fileService.LoadDocument(filePath, 1920, 1080));
                });

                if (loadResult is not { } result) return;

                // DEBUG: Dump raw toolkit data BEFORE any conversion issues
                if (_fileService.CurrentUIPage != null)
                {
                    _debugService.DumpToolkitData(_fileService.CurrentUIPage, filePath);
                }

                // Update design size
                _designWidth = result.DesignWidth;
                _designHeight = result.DesignHeight;
                UpdateCanvasSize();

                // Load elements into editor
                RootElements.Clear();
                foreach (var element in result.RootElements)
                {
                    RootElements.Add(element);
                }

                // DEBUG: Dump converted ViewModel data AFTER loading
                _debugService.DumpData("LOAD", RootElements, _designWidth, _designHeight, filePath);

                // IMPORTANT: Rebind TreeView to new hierarchy (VisibleHierarchy returns different collection now)
                VisualTreeView.ItemsSource = VisibleHierarchy;

                // Clear canvas and re-render
                RenderAllElements();

                // Document is now loaded - allow editing
                _isDocumentLoaded = true;

                MenuSave.IsEnabled = true;
                MenuSaveAs.IsEnabled = true;
                MenuUnloadPage.IsEnabled = true;
                QuickSaveButton.IsEnabled = true;

                var fileName = Path.GetFileNameWithoutExtension(filePath);
                UpdateTitleWithUnsavedIndicator();
                PageNameIndicator.Text = fileName;
                PageNameIndicator.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224));

                // Restore editor state (guides, reference image, etc.)
                RestorePageState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load UI page:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void QuickSaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Call the same save logic as the menu
            MenuSave_Click(sender, e);
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_fileService.CurrentFilePath))
            {
                MenuSaveAs_Click(sender, e);
                return;
            }

            try
            {
                // DEBUG: Dump data BEFORE saving
                _debugService.DumpData("SAVE_BEFORE", RootElements, _designWidth, _designHeight, _fileService.CurrentFilePath);

                // Save via file service with design resolution
                _fileService.SaveDocument(RootElements.ToList(), _designWidth, _designHeight);

                // Save editor state (guides, reference image, etc.)
                SaveCurrentPageState();
                UpdateRecentPagesMenu();

                // Update title to remove unsaved indicator
                UpdateTitleWithUnsavedIndicator();

                // Show saved indicator
                ShowSaveStatus("Saved!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save UI page:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (_connectedProject == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "Stride UI Page (*.sduipage)|*.sduipage",
                Title = "Save UI Page As",
                InitialDirectory = _connectedProject.AssetsPath,
                DefaultExt = ".sduipage"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Save via file service with design resolution
                    var filePath = _fileService.SaveDocumentAs(RootElements.ToList(), dialog.FileName, _designWidth, _designHeight);

                    MenuSave.IsEnabled = true;
                    QuickSaveButton.IsEnabled = true;
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    PageNameIndicator.Text = fileName;
                    PageNameIndicator.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224));

                    // Update title (will remove unsaved indicator since HasUnsavedChanges is now false)
                    UpdateTitleWithUnsavedIndicator();

                    // Save editor state (guides, reference image, etc.)
                    SaveCurrentPageState();
                    UpdateRecentPagesMenu();

                    // Show saved indicator
                    ShowSaveStatus("Saved!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save UI page:\n\n{ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void MenuImportTextures_Click(object sender, RoutedEventArgs e)
        {
            if (_connectedProject == null) return;

            var dialog = new Views.ImportAssetsDialog(_connectedProject.ProjectPath, Views.ImportAssetsDialog.ImportType.Textures);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                // Refresh project content to show newly imported assets
                await RefreshProjectAssetsAsync();
            }
        }

        private async void MenuImportFonts_Click(object sender, RoutedEventArgs e)
        {
            if (_connectedProject == null) return;

            var dialog = new Views.ImportAssetsDialog(_connectedProject.ProjectPath, Views.ImportAssetsDialog.ImportType.Fonts);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                // Refresh project content to show newly imported assets
                await RefreshProjectAssetsAsync();
            }
        }

        private async Task RefreshProjectAssetsAsync()
        {
            if (_connectedProject == null) return;

            try
            {
                await RunWithLoadingAsync("Refreshing assets...", async () =>
                {
                    // Reconnect to project to force toolkit to rescan assets
                    await Task.Run(() => _assetService.ConnectToProject(_connectedProject.ProjectPath));
                });

                // Now repopulate the content and restore folder filter (UI thread)
                PopulateProjectContent();
                RestoreContentBrowserState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to refresh assets:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowSaveStatus(string message)
        {
            SaveStatusIndicator.Text = message;

            // Clear after 2 seconds
            _saveStatusTimer?.Stop();
            _saveStatusTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _saveStatusTimer.Tick += (s, e) =>
            {
                SaveStatusIndicator.Text = "";
                _saveStatusTimer.Stop();
            };
            _saveStatusTimer.Start();
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Updates the Recent Projects submenu with the list of recent projects
        /// </summary>
        private void UpdateRecentProjectsMenu()
        {
            MenuRecentProjects.Items.Clear();

            var recentProjects = _editorDataService.GetRecentProjects();

            if (recentProjects.Count == 0)
            {
                var noProjectsItem = new MenuItem
                {
                    Header = "(No recent projects)",
                    IsEnabled = false
                };
                MenuRecentProjects.Items.Add(noProjectsItem);
                return;
            }

            // Add each recent project
            foreach (var project in recentProjects)
            {
                var projectName = Path.GetFileNameWithoutExtension(project.ProjectPath);
                var menuItem = new MenuItem
                {
                    Header = projectName,
                    ToolTip = project.ProjectPath,
                    Tag = project.ProjectPath
                };
                menuItem.Click += RecentProject_Click;
                MenuRecentProjects.Items.Add(menuItem);
            }

            // Add separator and clear option
            MenuRecentProjects.Items.Add(new Separator());

            var clearItem = new MenuItem
            {
                Header = "Clear Recent Projects"
            };
            clearItem.Click += ClearRecentProjects_Click;
            MenuRecentProjects.Items.Add(clearItem);
        }

        /// <summary>
        /// Handles clicking on a recent project to open it
        /// </summary>
        private async void RecentProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not string projectPath)
                return;

            // Check if the project file still exists
            if (!File.Exists(projectPath))
            {
                var result = MessageBox.Show(
                    $"Project file no longer exists:\n{projectPath}\n\nRemove from recent projects?\nThis will also remove all saved page states (guides, reference images) for this project.",
                    "Project Not Found",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _editorDataService.RemoveProjectData(projectPath);
                    UpdateRecentProjectsMenu();
                }
                return;
            }

            await ConnectToProjectAsync(projectPath);
        }

        /// <summary>
        /// Clears all recent projects from the list
        /// </summary>
        private void ClearRecentProjects_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all recent projects?\n\nThis will also remove all saved page states (guides, reference images) for ALL projects.",
                "Clear Recent Projects",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Remove all project data files
                foreach (var project in _editorDataService.GetRecentProjects())
                {
                    _editorDataService.RemoveProjectData(project.ProjectPath);
                }
                UpdateRecentProjectsMenu();
            }
        }

        /// <summary>
        /// Updates the Recent Pages submenu with pages from the current project
        /// </summary>
        private void UpdateRecentPagesMenu()
        {
            MenuRecentPages.Items.Clear();

            // Only show if project is connected
            if (string.IsNullOrEmpty(_connectedProjectSlnPath))
            {
                MenuRecentPages.IsEnabled = false;
                var noProjectItem = new MenuItem
                {
                    Header = "(No project connected)",
                    IsEnabled = false
                };
                MenuRecentPages.Items.Add(noProjectItem);
                return;
            }

            var projectData = _editorDataService.LoadProjectData(_connectedProjectSlnPath);
            if (projectData == null || projectData.Pages.Count == 0)
            {
                MenuRecentPages.IsEnabled = true;
                var noPagesItem = new MenuItem
                {
                    Header = "(No recent pages)",
                    IsEnabled = false
                };
                MenuRecentPages.Items.Add(noPagesItem);
                return;
            }

            MenuRecentPages.IsEnabled = true;

            // Sort pages by LastOpened (most recent first)
            var sortedPages = projectData.Pages.Values
                .OrderByDescending(p => p.LastOpened)
                .ToList();

            // Add each recent page
            foreach (var page in sortedPages)
            {
                var pageName = Path.GetFileNameWithoutExtension(page.PagePath);
                var menuItem = new MenuItem
                {
                    Header = pageName,
                    ToolTip = page.PagePath,
                    Tag = page.PagePath
                };
                menuItem.Click += RecentPage_Click;
                MenuRecentPages.Items.Add(menuItem);
            }

            // Add separator and clear option
            MenuRecentPages.Items.Add(new Separator());

            var clearItem = new MenuItem
            {
                Header = "Clear Recent Pages"
            };
            clearItem.Click += ClearRecentPages_Click;
            MenuRecentPages.Items.Add(clearItem);
        }

        /// <summary>
        /// Handles clicking on a recent page to open it
        /// </summary>
        private async void RecentPage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not string pagePath)
                return;

            // Check for unsaved changes before opening another document
            if (!CheckUnsavedChangesAndPrompt()) return;

            // Check if the page file still exists
            if (!File.Exists(pagePath))
            {
                MessageBox.Show(
                    $"Page file no longer exists:\n{pagePath}",
                    "Page Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            await LoadDocumentAsync(pagePath);
        }

        /// <summary>
        /// Clears recent pages for the current project
        /// </summary>
        private void ClearRecentPages_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_connectedProjectSlnPath))
                return;

            var result = MessageBox.Show(
                "Are you sure you want to clear all recent pages for this project?\n\nThis will remove saved page states (guides, reference images).",
                "Clear Recent Pages",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var projectData = _editorDataService.LoadProjectData(_connectedProjectSlnPath);
                if (projectData != null)
                {
                    projectData.Pages.Clear();
                    _editorDataService.SaveProjectData(projectData);
                }
                UpdateRecentPagesMenu();
            }
        }
    }
}

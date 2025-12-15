// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.IO;
using System.Text.Json;

namespace HS.Stride.UI.Editor.Core.Services
{
    /// <summary>
    /// Manages persistent editor settings like recent projects
    /// </summary>
    public class EditorSettingsService
    {
        private const string SettingsFileName = "editor_settings.json";
        private const int MaxRecentProjects = 10;

        private EditorSettings _settings;
        private readonly string _settingsPath;

        public EditorSettingsService()
        {
            // Store settings in Documents/HS Stride UI Editor
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var editorFolder = Path.Combine(documentsPath, "HS Stride UI Editor");

            if (!Directory.Exists(editorFolder))
                Directory.CreateDirectory(editorFolder);

            _settingsPath = Path.Combine(editorFolder, SettingsFileName);
            _settings = LoadSettings();
        }

        /// <summary>
        /// Gets the list of recent projects (most recent first)
        /// </summary>
        public List<RecentProject> RecentProjects => _settings.RecentProjects;

        /// <summary>
        /// Gets or sets the last used reference image path
        /// </summary>
        public string? LastReferenceImagePath
        {
            get => _settings.LastReferenceImagePath;
            set
            {
                _settings.LastReferenceImagePath = value;
                SaveSettings();
            }
        }

        /// <summary>
        /// Gets or sets the reference image opacity (0.0 - 1.0)
        /// </summary>
        public double ReferenceImageOpacity
        {
            get => _settings.ReferenceImageOpacity;
            set
            {
                _settings.ReferenceImageOpacity = Math.Clamp(value, 0.0, 1.0);
                SaveSettings();
            }
        }

        /// <summary>
        /// Gets or sets whether the window was maximized
        /// </summary>
        public bool IsMaximized
        {
            get => _settings.IsMaximized;
            set
            {
                _settings.IsMaximized = value;
                SaveSettings();
            }
        }

        /// <summary>
        /// Gets or sets the window bounds (Left, Top, Width, Height)
        /// </summary>
        public (double Left, double Top, double Width, double Height) WindowBounds
        {
            get => (_settings.WindowLeft, _settings.WindowTop, _settings.WindowWidth, _settings.WindowHeight);
            set
            {
                _settings.WindowLeft = value.Left;
                _settings.WindowTop = value.Top;
                _settings.WindowWidth = value.Width;
                _settings.WindowHeight = value.Height;
                SaveSettings();
            }
        }

        /// <summary>
        /// Add a project to the recent projects list
        /// </summary>
        public void AddRecentProject(string projectPath, string projectName)
        {
            // Remove if already exists (we'll re-add at top)
            _settings.RecentProjects.RemoveAll(p =>
                string.Equals(p.ProjectPath, projectPath, StringComparison.OrdinalIgnoreCase));

            // Add at the beginning
            _settings.RecentProjects.Insert(0, new RecentProject
            {
                ProjectPath = projectPath,
                ProjectName = projectName,
                LastOpened = DateTime.Now
            });

            // Keep only the most recent N projects
            if (_settings.RecentProjects.Count > MaxRecentProjects)
            {
                _settings.RecentProjects = _settings.RecentProjects.Take(MaxRecentProjects).ToList();
            }

            SaveSettings();
        }

        /// <summary>
        /// Remove a project from the recent list (e.g., if it no longer exists)
        /// </summary>
        public void RemoveRecentProject(string projectPath)
        {
            _settings.RecentProjects.RemoveAll(p =>
                string.Equals(p.ProjectPath, projectPath, StringComparison.OrdinalIgnoreCase));
            SaveSettings();
        }

        /// <summary>
        /// Clear all recent projects
        /// </summary>
        public void ClearRecentProjects()
        {
            _settings.RecentProjects.Clear();
            SaveSettings();
        }

        private EditorSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    return JsonSerializer.Deserialize<EditorSettings>(json) ?? new EditorSettings();
                }
            }
            catch
            {
                // If loading fails, return default settings
            }

            return new EditorSettings();
        }

        private void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
                // Silently fail - settings are not critical
            }
        }
    }

    /// <summary>
    /// Represents the editor settings data
    /// </summary>
    public class EditorSettings
    {
        public List<RecentProject> RecentProjects { get; set; } = new();
        public string? LastReferenceImagePath { get; set; }
        public double ReferenceImageOpacity { get; set; } = 0.3;

        // Window state
        public bool IsMaximized { get; set; } = false;
        public double WindowLeft { get; set; } = 100;
        public double WindowTop { get; set; } = 100;
        public double WindowWidth { get; set; } = 1600;
        public double WindowHeight { get; set; } = 900;
    }

    /// <summary>
    /// Represents a recent project entry
    /// </summary>
    public class RecentProject
    {
        public string ProjectPath { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public DateTime LastOpened { get; set; }
    }
}

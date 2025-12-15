// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HS.Stride.UI.Editor.Core.Models;

namespace HS.Stride.UI.Editor.Core.Services
{
    /// <summary>
    /// Service for managing editor-only data (project states, page states, etc.)
    /// stored in the user's Documents folder for easy access.
    /// </summary>
    public class EditorDataService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "HS Stride UI Editor");

        private static readonly string ProjectsFolder = Path.Combine(AppDataFolder, "Projects");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Ensures the AppData folders exist
        /// </summary>
        private static void EnsureFoldersExist()
        {
            Directory.CreateDirectory(AppDataFolder);
            Directory.CreateDirectory(ProjectsFolder);
        }

        /// <summary>
        /// Generates a safe filename from a project path using a hash
        /// </summary>
        private static string GetProjectFileName(string projectPath)
        {
            // Use hash to create a unique, filesystem-safe name
            var bytes = Encoding.UTF8.GetBytes(projectPath.ToLowerInvariant());
            var hash = SHA256.HashData(bytes);
            var hashString = Convert.ToHexString(hash)[..16]; // First 16 chars

            // Also include project name for readability
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            var safeName = string.Concat(projectName.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
            if (safeName.Length > 30) safeName = safeName[..30];

            return $"{safeName}_{hashString}.json";
        }

        /// <summary>
        /// Gets the full path to a project's data file
        /// </summary>
        private static string GetProjectFilePath(string projectPath)
        {
            return Path.Combine(ProjectsFolder, GetProjectFileName(projectPath));
        }

        /// <summary>
        /// Loads project data from disk, or returns null if not found
        /// </summary>
        public EditorProjectData? LoadProjectData(string projectPath)
        {
            try
            {
                var filePath = GetProjectFilePath(projectPath);
                if (!File.Exists(filePath))
                    return null;

                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<EditorProjectData>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load project data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves project data to disk
        /// </summary>
        public void SaveProjectData(EditorProjectData projectData)
        {
            try
            {
                EnsureFoldersExist();
                var filePath = GetProjectFilePath(projectData.ProjectPath);
                var json = JsonSerializer.Serialize(projectData, JsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save project data: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets or creates project data, updating LastOpened
        /// </summary>
        public EditorProjectData GetOrCreateProjectData(string projectPath)
        {
            var data = LoadProjectData(projectPath);
            if (data == null)
            {
                data = new EditorProjectData { ProjectPath = projectPath };
            }
            data.LastOpened = DateTime.Now;
            return data;
        }

        /// <summary>
        /// Gets all recent projects, sorted by LastOpened (most recent first)
        /// </summary>
        public List<EditorProjectData> GetRecentProjects()
        {
            var projects = new List<EditorProjectData>();

            try
            {
                EnsureFoldersExist();
                var files = Directory.GetFiles(ProjectsFolder, "*.json");

                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var project = JsonSerializer.Deserialize<EditorProjectData>(json, JsonOptions);
                        if (project != null && !string.IsNullOrEmpty(project.ProjectPath))
                        {
                            projects.Add(project);
                        }
                    }
                    catch
                    {
                        // Skip invalid files
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get recent projects: {ex.Message}");
            }

            // Sort by LastOpened, most recent first
            return projects.OrderByDescending(p => p.LastOpened).ToList();
        }

        /// <summary>
        /// Removes a project's data file
        /// </summary>
        public bool RemoveProjectData(string projectPath)
        {
            try
            {
                var filePath = GetProjectFilePath(projectPath);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to remove project data: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Gets the number of pages with saved state for a project
        /// </summary>
        public int GetPageCount(string projectPath)
        {
            var data = LoadProjectData(projectPath);
            return data?.Pages.Count ?? 0;
        }

        /// <summary>
        /// Saves page state within a project
        /// </summary>
        public void SavePageState(string projectPath, PageEditorState pageState)
        {
            var projectData = GetOrCreateProjectData(projectPath);
            projectData.Pages[pageState.PagePath] = pageState;
            SaveProjectData(projectData);
        }

        /// <summary>
        /// Loads page state for a specific page, or returns null if not found
        /// </summary>
        public PageEditorState? LoadPageState(string projectPath, string pagePath)
        {
            var projectData = LoadProjectData(projectPath);
            if (projectData?.Pages.TryGetValue(pagePath, out var pageState) == true)
            {
                return pageState;
            }
            return null;
        }
    }
}

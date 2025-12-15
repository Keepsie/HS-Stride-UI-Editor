// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace HS.Stride.UI.Editor.Core.Models
{
    /// <summary>
    /// Editor-only data for a project, saved to AppData folder.
    /// Contains project info and all page states.
    /// </summary>
    public class EditorProjectData
    {
        /// <summary>
        /// Full path to the project file (.sln or .csproj)
        /// </summary>
        public string ProjectPath { get; set; } = "";

        /// <summary>
        /// When this project was last opened
        /// </summary>
        public DateTime LastOpened { get; set; } = DateTime.Now;

        /// <summary>
        /// Page-specific editor state, keyed by page file path
        /// </summary>
        public Dictionary<string, PageEditorState> Pages { get; set; } = new();

        /// <summary>
        /// Content browser folder filter (relative path from Assets folder)
        /// </summary>
        public string? ContentBrowserFolderFilter { get; set; }

        /// <summary>
        /// Gets or creates page state for the given path
        /// </summary>
        public PageEditorState GetOrCreatePage(string pagePath)
        {
            if (!Pages.TryGetValue(pagePath, out var page))
            {
                page = new PageEditorState { PagePath = pagePath };
                Pages[pagePath] = page;
            }
            page.LastOpened = DateTime.Now;
            return page;
        }
    }

    /// <summary>
    /// Editor state for a specific UI page
    /// </summary>
    public class PageEditorState
    {
        /// <summary>
        /// Full path to the page file
        /// </summary>
        public string PagePath { get; set; } = "";

        /// <summary>
        /// When this page was last opened
        /// </summary>
        public DateTime LastOpened { get; set; } = DateTime.Now;

        /// <summary>
        /// Guide lines saved for this page
        /// </summary>
        public List<GuideData> Guides { get; set; } = new();

        /// <summary>
        /// Reference image path
        /// </summary>
        public string? ReferenceImagePath { get; set; }

        /// <summary>
        /// Reference image opacity (0.0 - 1.0)
        /// </summary>
        public double ReferenceImageOpacity { get; set; } = 0.3;

        /// <summary>
        /// Editor-only element properties, keyed by element name
        /// </summary>
        public Dictionary<string, ElementEditorState> Elements { get; set; } = new();
    }

    /// <summary>
    /// Editor-only state for a specific UI element
    /// </summary>
    public class ElementEditorState
    {
        /// <summary>
        /// Element is locked (cannot be selected on canvas, only via hierarchy)
        /// </summary>
        public bool IsLocked { get; set; } = false;

        /// <summary>
        /// Element can be positioned outside the canvas bounds
        /// </summary>
        public bool AllowCanvasOverflow { get; set; } = false;
    }

    /// <summary>
    /// Data for a single guide line
    /// </summary>
    public class GuideData
    {
        /// <summary>
        /// True if horizontal, false if vertical
        /// </summary>
        public bool IsHorizontal { get; set; }

        /// <summary>
        /// Position in canvas coordinates
        /// </summary>
        public double Position { get; set; }
    }
}

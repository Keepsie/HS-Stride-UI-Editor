// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.IO;
using System.Text.Json;
using HS.Stride.UI.Editor.ViewModels;
using HS.Stride.Editor.Toolkit.Core.UIPageEditing;
using ToolkitUIElement = HS.Stride.Editor.Toolkit.Core.UIPageEditing.UIElement;

namespace HS.Stride.UI.Editor.Core.Services
{
    /// <summary>
    /// Debug data dumping service for troubleshooting element data on load/save.
    /// Set DEBUG_DUMP_DATA to true to enable JSON dumps to desktop.
    /// </summary>
    public class DebugService
    {
        private const bool DEBUG_DUMP_DATA = false;
        private static readonly string DEBUG_OUTPUT_PATH = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "UIEditorDebug");

        /// <summary>
        /// Dumps element data to JSON for debugging. Call on load/save.
        /// </summary>
        public void DumpData(string operation, IEnumerable<UIElementViewModel> rootElements,
            double designWidth, double designHeight, string? sourceFile = null)
        {
            if (!DEBUG_DUMP_DATA) return;

            try
            {
                Directory.CreateDirectory(DEBUG_OUTPUT_PATH);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"{operation}_{timestamp}.json";
                var filepath = Path.Combine(DEBUG_OUTPUT_PATH, filename);

                var debugData = new
                {
                    Operation = operation,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    SourceFile = sourceFile ?? "N/A",
                    DesignResolution = new { Width = designWidth, Height = designHeight },
                    ElementCount = rootElements.Count(),
                    Elements = rootElements.Select(e => DumpElementRecursive(e)).ToList()
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(debugData, options);
                File.WriteAllText(filepath, json);

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Data dumped to: {filepath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Failed to dump data: {ex.Message}");
            }
        }

        /// <summary>
        /// Dumps raw toolkit element data before conversion
        /// </summary>
        public void DumpToolkitData(UIPage uiPage, string sourceFile)
        {
            if (!DEBUG_DUMP_DATA) return;

            try
            {
                Directory.CreateDirectory(DEBUG_OUTPUT_PATH);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"TOOLKIT_RAW_{timestamp}.json";
                var filepath = Path.Combine(DEBUG_OUTPUT_PATH, filename);

                var resolution = uiPage.Resolution;
                var debugData = new
                {
                    SourceFile = sourceFile,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Resolution = new
                    {
                        X = resolution.TryGetValue("X", out var x) ? x : 0,
                        Y = resolution.TryGetValue("Y", out var y) ? y : 0,
                        Z = resolution.TryGetValue("Z", out var z) ? z : 0
                    },
                    RootElementCount = uiPage.RootElements.Count,
                    TotalElementCount = uiPage.AllElements.Count,
                    Elements = uiPage.AllElements.Select(e => DumpToolkitElement(e)).ToList()
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(debugData, options);
                File.WriteAllText(filepath, json);

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Toolkit data dumped to: {filepath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Failed to dump toolkit data: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively dump element data for debugging
        /// </summary>
        private object DumpElementRecursive(UIElementViewModel element)
        {
            return new
            {
                element.Id,
                element.Name,
                element.ElementType,
                element.IsSystemElement,
                Position = new { element.X, element.Y },
                Size = new { element.Width, element.Height },
                Alignment = new { Horizontal = element.HorizontalAlignment, Vertical = element.VerticalAlignment },
                element.Opacity,
                element.Visibility,
                BackgroundColor = $"#{element.BackgroundColor.A:X2}{element.BackgroundColor.R:X2}{element.BackgroundColor.G:X2}{element.BackgroundColor.B:X2}",
                TypeSpecific = GetTypeSpecificProperties(element),
                ChildCount = element.Children.Count,
                Children = element.Children.Select(c => DumpElementRecursive(c)).ToList()
            };
        }

        /// <summary>
        /// Get type-specific properties for debug dump
        /// </summary>
        private object? GetTypeSpecificProperties(UIElementViewModel element)
        {
            return element.ElementType switch
            {
                "TextBlock" => new { element.Text, element.FontSize, element.TextAlignment },
                "ImageElement" => new { element.ImageSource, element.StretchType, element.SpriteFrame },
                "Button" => new { element.ButtonText },
                "Grid" => new { element.RowDefinitions, element.ColumnDefinitions },
                "StackPanel" => new { Orientation = element.StackPanelOrientation },
                _ => null
            };
        }

        /// <summary>
        /// Dump a single toolkit element's data
        /// </summary>
        private object DumpToolkitElement(ToolkitUIElement element)
        {
            var margin = element.GetMargin();
            var parentDims = element.GetParentDimensions();

            return new
            {
                element.Id,
                element.Name,
                element.Type,
                ParentId = element.Parent?.Id ?? "ROOT",
                ParentDimensions = new { Width = parentDims.Width, Height = parentDims.Height },
                RawMargin = new { margin.Left, margin.Top, margin.Right, margin.Bottom },
                WidthRaw = element.Get<float?>("Width"),
                WidthHelper = element.GetWidth(),
                HeightRaw = element.Get<float?>("Height"),
                HeightHelper = element.GetHeight(),
                HorizontalAlignment = element.Get<string>("HorizontalAlignment") ?? "Stretch",
                VerticalAlignment = element.Get<string>("VerticalAlignment") ?? "Stretch",
                Opacity = element.Get<float?>("Opacity"),
                Text = element.Get<string>("Text"),
                TextSize = element.Get<float?>("TextSize"),
                HasSource = element.GetSpriteSheet("Source") != null
            };
        }
    }
}

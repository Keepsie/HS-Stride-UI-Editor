// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HS.Stride.UI.Editor.Controls;
using HS.Stride.UI.Editor.Core.Abstractions;
using HS.Stride.UI.Editor.Core.Models;
using HS.Stride.UI.Editor.Core.Services;
using HS.Stride.UI.Editor.Models;
using HS.Stride.UI.Editor.Models.Commands;
using HS.Stride.UI.Editor.ViewModels;
using HS.Stride.UI.Editor.Views;
using HS.Stride.Editor.Toolkit.Core;
using HS.Stride.Editor.Toolkit.Core.UIPageEditing;
using HS.Stride.Editor.Toolkit.Core.AssetEditing;
using Microsoft.Win32;
using ToolkitUIElement = HS.Stride.Editor.Toolkit.Core.UIPageEditing.UIElement;
using System.IO;

namespace HS.Stride.UI.Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<UIElementFactory> UILibrary { get; set; } = new();
        public ObservableCollection<UIElementViewModel> RootElements { get; set; } = new();

        // Property that exposes visible hierarchy (children of root Grid, not the Grid itself)
        public ObservableCollection<UIElementViewModel> VisibleHierarchy
        {
            get
            {
                if (RootElements.Count > 0 && RootElements[0].IsSystemElement)
                {
                    return RootElements[0].Children;
                }
                return RootElements;
            }
        }

        // All selected elements
        private List<UIElementViewModel> _selectedElements = new();
        // Only "root" selected elements (elements whose parents are NOT selected) - used for drag operations
        private List<UIElementViewModel> _selectedRootElements = new();
        private double _zoomLevel = 1.0;
        private const double ZoomMin = 0.1;
        private const double ZoomMax = 5.0;
        private const double ZoomStep = 0.1;
        private double _designWidth = 1280;
        private double _designHeight = 720;

        // Pan state
        private bool _isPanning;
        private Point _panStartPoint;
        private double _panStartScrollH;
        private double _panStartScrollV;

        // Photoshop-style shortcuts state
        private bool _isSpaceHeld = false;

        // TreeView drag state
        private bool _isTreeViewDragging;
        private Point _treeViewDragStartPoint;

        // Snap settings
        private bool _snapToGrid = true;
        private bool _snapToPixel = true;

        // Guides
        private List<Controls.Guide> _guides = new();
        private Canvas? _guidesCanvas;

        // Undo/Redo
        private UndoRedoManager _undoRedoManager = new();

        // Element naming counters
        private Dictionary<string, int> _elementCounters = new();

        // Toolkit integration
        private StrideProject? _connectedProject;
        private string? _connectedProjectSlnPath; // Store the .sln file path for recent projects

        // Document state - must have a document loaded (new or opened) before editing
        private bool _isDocumentLoaded = false;

        // Selection box (marquee selection)
        private bool _isDrawingSelectionBox = false;
        private Point _selectionBoxStart;

        // Flag to prevent recursive selection events
        private bool _isUpdatingTreeViewSelection = false;

        // Multi-select drag tracking for undo support
        private bool _isMultiSelectDragActive = false;
        private Dictionary<string, (double X, double Y)> _multiSelectDragStartPositions = new();

        // Core Services
        private FileOperationService _fileService = new();
        private AssetService _assetService = new();
        private DragDropService _dragDropService;
        private CanvasRenderService _renderService = new();
        private EditorSettingsService _settingsService = new();
        private EditorDataService _editorDataService = new();
        private DebugService _debugService = new();
        private AlignmentService _alignmentService = new();

        // Current reference image path (per-page, saved to editor data)
        private string? _currentReferenceImagePath;

        // Full list of project assets (for search filtering)
        private List<AssetItem>? _allProjectAssets;

        // Current folder filter for assets (null = show all)
        private string? _currentFolderFilter;

        // Save status timer
        private System.Windows.Threading.DispatcherTimer? _saveStatusTimer;

        /// <summary>
        /// Generate a clean, sequential name for a UI element
        /// </summary>
        private string GenerateElementName(string elementType)
        {
            if (!_elementCounters.ContainsKey(elementType))
            {
                _elementCounters[elementType] = 1;
            }
            else
            {
                _elementCounters[elementType]++;
            }

            return $"{elementType}{_elementCounters[elementType]}";
        }

        #region Loading Overlay

        /// <summary>
        /// Show loading overlay with a message
        /// </summary>
        private void ShowLoadingOverlay(string message)
        {
            LoadingText.Text = message;
            LoadingOverlay.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Hide loading overlay
        /// </summary>
        private void HideLoadingOverlay()
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Execute an action with loading overlay shown
        /// </summary>
        private async Task RunWithLoadingAsync(string message, Func<Task> action)
        {
            ShowLoadingOverlay(message);
            try
            {
                await action();
            }
            finally
            {
                HideLoadingOverlay();
            }
        }

        /// <summary>
        /// Execute a function with loading overlay shown and return result
        /// </summary>
        private async Task<T> RunWithLoadingAsync<T>(string message, Func<Task<T>> action)
        {
            ShowLoadingOverlay(message);
            try
            {
                return await action();
            }
            finally
            {
                HideLoadingOverlay();
            }
        }

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            // Restore window state from settings
            RestoreWindowState();

            // Set window title with version from assembly
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"HS Stride UI Editor {version?.Major}.{version?.Minor}.{version?.Build} - © 2025 Happenstance Games LLC";

            // Initialize services
            _dragDropService = new DragDropService(GenerateElementName);

            // Initialize render service with ArtboardCanvas - elements live directly in artboard coordinates
            _renderService.Initialize(ArtboardCanvas, EmptyStateText);
            _renderService.LoadAssetImageCallback = LoadAssetImage;
            _renderService.LoadFontCallback = LoadFont;
            _renderService.SnapToGridCallback = SnapToGrid;
            _renderService.GetArtboardBoundsCallback = () => (_designWidth, _designHeight);
            _renderService.GetZoomLevelCallback = () => _zoomLevel;
            // No more artboard offset callback - artboard IS the coordinate system now
            _renderService.ElementSelectedHandler = Visual_ElementSelected;
            _renderService.ElementChangedHandler = Visual_ElementChanged;
            _renderService.DragStateChangedHandler = Visual_DragStateChanged;
            _renderService.AltDragDuplicateHandler = Visual_AltDragDuplicate;
            _renderService.UpdateScrollbarVisibilityCallback = UpdateScrollbarVisibility;
            _renderService.ElementContextMenu = (ContextMenu)Resources["ElementContextMenu"];
            _renderService.HandleMultiSelectDragCallback = HandleMultiSelectDrag;
            _renderService.CenterSnapCallback = GetCenterSnapPosition;

            // Initialize spacing guide overlay
            SpacingGuideOverlay.SetArtboardSize(_designWidth, _designHeight);

            InitializeUILibrary();
            // Don't create root container at startup - only when document is loaded (new/open)

            // Bind data to UI
            var collectionView = (System.Windows.Data.CollectionViewSource)Resources["UILibraryGrouped"];
            collectionView.Source = UILibrary;
            VisualTreeView.ItemsSource = VisibleHierarchy;

            // Wire up events
            VisualTreeView.SelectedItemChanged += VisualTreeView_SelectedItemChanged;
            VisualTreeView.Drop += VisualTreeView_Drop;
            VisualTreeView.DragOver += VisualTreeView_DragOver;
            VisualTreeView.DragLeave += (s, e) => { _lastHoveredItem = null; HideDropIndicator(); };
            VisualTreeView.PreviewMouseLeftButtonDown += VisualTreeView_PreviewMouseLeftButtonDown;
            VisualTreeView.PreviewMouseLeftButtonUp += VisualTreeView_PreviewMouseLeftButtonUp;
            VisualTreeView.MouseMove += VisualTreeView_MouseMove;
            UILibraryListBox.MouseMove += UILibraryListBox_MouseMove;
            EditorCanvas.Drop += EditorCanvas_Drop;
            EditorCanvas.DragOver += EditorCanvas_DragOver;
            EditorCanvas.AllowDrop = true;

            // Enable deselection by clicking on empty canvas and selection box
            RootArtboard.MouseLeftButtonDown += RootArtboard_MouseLeftButtonDown;
            ArtboardCanvas.MouseLeftButtonDown += ArtboardCanvas_MouseLeftButtonDown;
            ArtboardCanvas.MouseMove += ArtboardCanvas_MouseMove;
            ArtboardCanvas.MouseLeftButtonUp += ArtboardCanvas_MouseLeftButtonUp;

            // Project content drag-drop
            ProjectContentListView.MouseMove += ProjectContentListView_MouseMove;

            // Render initial elements
            _renderService.RenderAllElements(RootElements);

            // Keyboard shortcuts
            PreviewKeyDown += MainWindow_PreviewKeyDown;
            PreviewKeyUp += MainWindow_PreviewKeyUp;

            // Zoom controls
            ZoomInButton.Click += (s, e) => Zoom(ZoomStep);
            ZoomOutButton.Click += (s, e) => Zoom(-ZoomStep);
            ResetZoomButton.Click += (s, e) => SetZoom(1.0);
            CenterViewButton.Click += CenterViewButton_Click;
            CanvasScrollViewer.PreviewMouseWheel += CanvasScrollViewer_PreviewMouseWheel;

            // Middle mouse pan
            CanvasScrollViewer.PreviewMouseDown += CanvasScrollViewer_PreviewMouseDown;
            CanvasScrollViewer.PreviewMouseMove += CanvasScrollViewer_PreviewMouseMove;
            CanvasScrollViewer.PreviewMouseUp += CanvasScrollViewer_PreviewMouseUp;

            // Ruler updates
            CanvasScrollViewer.ScrollChanged += CanvasScrollViewer_ScrollChanged;

            // Guide creation from rulers
            HorizontalRuler.MouseLeftButtonDown += HorizontalRuler_MouseLeftButtonDown;
            VerticalRuler.MouseLeftButtonDown += VerticalRuler_MouseLeftButtonDown;

            // Settings button
            SettingsButton.Click += SettingsButton_Click;

            // Set initial canvas size
            UpdateCanvasSize();

            // Center artboard and zoom to fit
            CenterAndFitArtboard();

            // Wire up PropertyPanel to get project assets
            PropertiesPanel.GetProjectAssets = GetProjectAssetsForPicker;

            // Track property changes from PropertyPanel for unsaved changes warning
            PropertiesPanel.PropertyChanged += (s, e) => MarkDocumentAsChanged();

            // Handle property changes from PropertyPanel for undo support
            PropertiesPanel.PropertyChangeCommitted += PropertiesPanel_PropertyChangeCommitted;

            // Update scrollbar visibility when window resizes or canvas changes
            this.SizeChanged += MainWindow_SizeChanged;
            CanvasScrollViewer.SizeChanged += CanvasScrollViewer_SizeChanged;

            // Load recent projects/pages menus
            UpdateRecentProjectsMenu();
            UpdateRecentPagesMenu();

            // Restore reference image opacity from settings
            RefImageOpacitySlider.Value = _settingsService.ReferenceImageOpacity * 100;

            // Track document changes for unsaved changes warning
            _undoRedoManager.DocumentChanged += (s, e) => MarkDocumentAsChanged();

            // Handle window closing to warn about unsaved changes
            Closing += MainWindow_Closing;
        }

        private List<AssetItem> GetProjectAssetsForPicker()
        {
            return _assetService.GetProjectAssetsList();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog();
            dialog.Owner = this;

            // Load current settings
            dialog.DesignWidth = _designWidth;
            dialog.DesignHeight = _designHeight;
            dialog.ShowGrid = _showGrid;
            dialog.GuideColor = _guideColor;
            dialog.GuideThickness = _guideThickness;
            dialog.SelectionColor = _selectionColor;
            dialog.SelectionThickness = _selectionThickness;
            dialog.HighlightColor = _highlightColor;

            if (dialog.ShowDialog() == true)
            {
                // Apply new settings
                bool resolutionChanged = _designWidth != dialog.DesignWidth || _designHeight != dialog.DesignHeight;
                _designWidth = dialog.DesignWidth;
                _designHeight = dialog.DesignHeight;
                _showGrid = dialog.ShowGrid;
                _guideColor = dialog.GuideColor;
                _guideThickness = dialog.GuideThickness;
                _selectionColor = dialog.SelectionColor;
                _selectionThickness = dialog.SelectionThickness;
                _highlightColor = dialog.HighlightColor;

                // Update visuals
                if (resolutionChanged)
                {
                    UpdateCanvasSize();
                    MarkDocumentAsChanged();
                }
                UpdateGridVisibility();
                UpdateGuideColors();
                UpdateSelectionColors();
            }
        }

        // Visual settings
        private bool _showGrid = true;
        private Color _guideColor = Color.FromRgb(0, 150, 255);
        private double _guideThickness = 1;
        private Color _selectionColor = Colors.Blue;
        private double _selectionThickness = 2;
        private Color _highlightColor = Color.FromRgb(100, 150, 255);

        private void UpdateGridVisibility()
        {
            if (GridBackground != null)
            {
                GridBackground.Visibility = _showGrid ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateGuideColors()
        {
            foreach (var guide in _guides)
            {
                // Guide colors would need to be updated in the Guide class
                // For now, this is a placeholder
            }
        }

        private void UpdateSelectionColors()
        {
            // Selection colors would need to be updated in UIElementVisual
            // For now, this is a placeholder
        }

        private void CenterViewButton_Click(object sender, RoutedEventArgs e)
        {
            // Direct center - no timing issues since user clicked it
            var scrollableWidth = CanvasScrollViewer.ScrollableWidth;
            var scrollableHeight = CanvasScrollViewer.ScrollableHeight;

            CanvasScrollViewer.ScrollToHorizontalOffset(scrollableWidth / 2);
            CanvasScrollViewer.ScrollToVerticalOffset(scrollableHeight / 2);
        }

        private void UpdateCanvasSize()
        {
            // Update artboard to match design size
            if (RootArtboard != null)
            {
                RootArtboard.Width = _designWidth;
                RootArtboard.Height = _designHeight;
            }

            // Update size label
            if (ArtboardSizeLabel != null)
            {
                ArtboardSizeLabel.Text = $"{_designWidth} × {_designHeight}";
            }

            // Update spacing guide overlay dimensions
            SpacingGuideOverlay?.SetArtboardSize(_designWidth, _designHeight);

            // Recalculate canvas size and reposition artboard
            CenterArtboard();
            UpdateScrollbarVisibility();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScrollbarVisibility();

            // Re-center artboard when window is resized
            if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    CenterArtboard();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void CanvasScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScrollbarVisibility();
        }

        private void UpdateScrollbarVisibility()
        {
            if (CanvasScrollViewer == null || EditorCanvas == null) return;

            // Always show scrollbars since we have a large canvas for panning
            // They'll only be active when there's actually content to scroll to
            CanvasScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            CanvasScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        private void CenterAndFitArtboard()
        {
            // Wait for the window to be fully loaded and layout to be calculated
            this.Loaded += (s, e) =>
            {
                // Multiple attempts to ensure we get the correct viewport size
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    FitArtboardToViewport();
                }), System.Windows.Threading.DispatcherPriority.Loaded);

                // Second attempt with lower priority to catch any remaining layout updates
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    FitArtboardToViewport();
                }), System.Windows.Threading.DispatcherPriority.Input);
            };
        }

        private void FitArtboardToViewport()
        {
            if (CanvasScrollViewer == null || RootArtboard == null || EditorCanvas == null) return;

            // Get viewport dimensions (available space for artboard)
            var viewportWidth = CanvasScrollViewer.ActualWidth;
            var viewportHeight = CanvasScrollViewer.ActualHeight;

            // If viewport is still 0 (layout not complete), try again with a delay
            if (viewportWidth <= 0 || viewportHeight <= 0)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    FitArtboardToViewport();
                }), System.Windows.Threading.DispatcherPriority.Background);
                return;
            }

            // Get artboard dimensions (design size)
            var artboardWidth = _designWidth;
            var artboardHeight = _designHeight;

            // Calculate zoom to fit artboard with some padding
            const double padding = 80; // 40px padding on each side
            double scaleX = (viewportWidth - padding) / artboardWidth;
            double scaleY = (viewportHeight - padding) / artboardHeight;
            double fitZoom = Math.Min(scaleX, scaleY);

            // Clamp zoom to reasonable bounds (don't zoom in beyond 100%)
            fitZoom = Math.Clamp(fitZoom, 0.1, 1.0);

            // Apply the zoom (this will call CenterArtboard)
            SetZoom(fitZoom);
        }

        private void CenterArtboard()
        {
            if (EditorCanvas == null || RootArtboard == null || CanvasScrollViewer == null) return;

            // Get viewport dimensions
            var viewportWidth = CanvasScrollViewer.ActualWidth;
            var viewportHeight = CanvasScrollViewer.ActualHeight;

            if (viewportWidth <= 0 || viewportHeight <= 0) return;

            var artboardWidth = _designWidth;
            var artboardHeight = _designHeight;

            // Canvas needs to be large enough to:
            // - Contain the artboard
            // - Allow scrolling to center even small artboards
            // - Allow scrolling to see edges of large artboards
            // Size = artboard + viewport on each side (in canvas coords, before zoom)
            var canvasWidth = artboardWidth + (viewportWidth / _zoomLevel) * 2;
            var canvasHeight = artboardHeight + (viewportHeight / _zoomLevel) * 2;

            EditorCanvas.Width = canvasWidth;
            EditorCanvas.Height = canvasHeight;

            // Position artboard in center of canvas
            var artboardX = (canvasWidth - artboardWidth) / 2;
            var artboardY = (canvasHeight - artboardHeight) / 2;
            Canvas.SetLeft(RootArtboard, artboardX);
            Canvas.SetTop(RootArtboard, artboardY);

            // Position size label above artboard
            if (ArtboardSizeLabel != null)
            {
                Canvas.SetLeft(ArtboardSizeLabel, artboardX);
                Canvas.SetTop(ArtboardSizeLabel, artboardY - 30);
            }

            // Need to wait for layout to complete before scrolling
            // Otherwise scroll extent isn't calculated yet
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                // Scroll to center - middle of the scroll range
                var scrollableWidth = CanvasScrollViewer.ScrollableWidth;
                var scrollableHeight = CanvasScrollViewer.ScrollableHeight;

                CanvasScrollViewer.ScrollToHorizontalOffset(scrollableWidth / 2);
                CanvasScrollViewer.ScrollToVerticalOffset(scrollableHeight / 2);

                // Update rulers after scroll
                UpdateRulers();
            }), System.Windows.Threading.DispatcherPriority.Loaded);

            UpdateScrollbarVisibility();
        }

        private void CanvasScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Zoom with Ctrl+Wheel or Alt+Wheel (Photoshop style)
            if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Alt)
            {
                Zoom(e.Delta > 0 ? ZoomStep : -ZoomStep);
                e.Handled = true;
            }
            // Horizontal scroll with Shift+Wheel
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                var newOffset = CanvasScrollViewer.HorizontalOffset - e.Delta;
                CanvasScrollViewer.ScrollToHorizontalOffset(newOffset);
                e.Handled = true;
            }
        }

        // Keyboard input handlers moved to MainWindow.Input.cs

        private void InitializeUILibrary()
        {
            // Containers (Core - Always useful)
            UILibrary.Add(new UIElementFactory("Canvas", "Canvas", "Containers", "Absolute positioning container"));
            UILibrary.Add(new UIElementFactory("Stack Panel", "StackPanel", "Containers", "Stack layout container"));
            UILibrary.Add(new UIElementFactory("Scroll Viewer", "ScrollViewer", "Containers", "Scrollable container"));

            // Controls (Core - Always useful)
            UILibrary.Add(new UIElementFactory("Text Block", "TextBlock", "Controls", "Text label"));
            UILibrary.Add(new UIElementFactory("Button", "Button", "Controls", "Button control"));
            UILibrary.Add(new UIElementFactory("Edit Text", "EditText", "Controls", "Text input field"));
            UILibrary.Add(new UIElementFactory("Slider", "Slider", "Controls", "Slider control"));
            UILibrary.Add(new UIElementFactory("Toggle Button", "ToggleButton", "Controls", "Toggle/checkbox button"));

            // Visual Elements
            UILibrary.Add(new UIElementFactory("Image", "ImageElement", "Visual", "Image from texture"));
            UILibrary.Add(new UIElementFactory("Modal", "ModalElement", "Visual", "Modal overlay"));
        }

        private void InitializeRootContainer()
        {
            // Always start with a hidden root Grid container (like Stride's structure)
            var rootGrid = new UIElementViewModel("RootGrid", "Grid")
            {
                Width = _designWidth,
                Height = _designHeight,
                X = 0,
                Y = 0,
                IsSystemElement = true // Hidden from hierarchy and canvas
            };
            RootElements.Add(rootGrid);
        }

       

        #region Rendering

        private void RenderAllElements()
        {
            _renderService.RenderAllElements(RootElements);
        }

        private void RenderElement(UIElementViewModel element)
        {
            _renderService.RenderElement(element);
        }

        private ImageSource? LoadAssetImage(string assetName, string elementType, int spriteFrame, Color tintColor)
        {
            // For sprite sheets, get the specific frame cropped from the sheet
            // For textures, just return the whole image
            var imageSource = _assetService.GetSpriteFrame(assetName, spriteFrame);
            var result = imageSource?.NativeSource as ImageSource;

            // Apply tint color if not white (Stride's multiplicative blend)
            if (result != null && (tintColor.R != 255 || tintColor.G != 255 || tintColor.B != 255 || tintColor.A != 255))
            {
                result = _assetService.ApplyTintToImage(result, tintColor);
            }

            return result;
        }

        private FontFamily? LoadFont(string fontAssetReference)
        {
            // Load font from Stride project asset
            return _assetService.LoadFont(fontAssetReference);
        }

        private void Visual_ElementSelected(object? sender, ElementSelectedEventArgs e)
        {
            SelectElement(e.Element, e.CtrlPressed);
        }

        private void Visual_ElementChanged(object? sender, ElementChangedEventArgs e)
        {
            // Element was moved/resized, create undo command
            var element = e.Element;

            // Check if this was a multi-select drag
            if (_isMultiSelectDragActive && !e.IsResize)
            {
                // Create batch move command for all selected elements
                var moves = new List<(UIElementViewModel, double, double, double, double)>();

                foreach (var selElement in _selectedRootElements)
                {
                    if (_multiSelectDragStartPositions.TryGetValue(selElement.Id, out var startPos))
                    {
                        // Only include if position changed
                        if (selElement.X != startPos.X || selElement.Y != startPos.Y)
                        {
                            moves.Add((selElement, startPos.X, startPos.Y, selElement.X, selElement.Y));
                        }
                    }
                }

                if (moves.Count > 0)
                {
                    var command = new BatchMoveCommand(moves, "Move Elements");
                    _undoRedoManager.RecordExecuted(command);
                }

                // Reset multi-select drag state
                _isMultiSelectDragActive = false;
                _multiSelectDragStartPositions.Clear();
            }
            else
            {
                // Single element operation
                // Only create command if position/size actually changed
                bool posChanged = element.X != e.OldX || element.Y != e.OldY;
                bool sizeChanged = element.Width != e.OldWidth || element.Height != e.OldHeight;

                if (e.IsResize && (posChanged || sizeChanged))
                {
                    var command = new ResizeElementCommand(
                        element,
                        e.OldX, e.OldY, e.OldWidth, e.OldHeight,
                        element.X, element.Y, element.Width, element.Height);
                    _undoRedoManager.RecordExecuted(command);
                }
                else if (!e.IsResize && posChanged)
                {
                    var command = new MoveElementCommand(
                        element,
                        e.OldX, e.OldY,
                        element.X, element.Y);
                    _undoRedoManager.RecordExecuted(command);
                }
            }

            // Update property panel
            if (_selectedElements.Contains(element))
            {
                UpdatePropertyPanel();
            }
        }

        private void Visual_AltDragDuplicate(object? sender, UIElementViewModel element)
        {
            // Alt+drag to duplicate (Photoshop style)
            // Create a clone at the original position - the dragged element will move away
            var duplicate = element.Clone(GenerateElementName(element.ElementType));

            // Keep the clone at the original position (the one being dragged will move)
            // The clone stays where the original was

            // Add to same parent
            var parent = element.Parent ?? (RootElements.Count > 0 ? RootElements[0] : null);

            var command = new CreateElementCommand(
                duplicate,
                parent,
                RootElements,
                RenderElement,
                RemoveElementVisual,
                el => { }); // Don't select the clone
            _undoRedoManager.Execute(command);
        }

        private void Visual_DragStateChanged(object? sender, DragStateEventArgs e)
        {
            if (e.IsDragging || e.IsResizing)
            {
                // Show spacing guides during drag/resize
                // Get all elements at the same level (siblings)
                var allElements = GetAllNonSystemElements();
                SpacingGuideOverlay.ShowGuides(e.Element, allElements);
            }
            else
            {
                // Hide guides when drag/resize ends
                SpacingGuideOverlay.HideGuides();
            }
        }

        /// <summary>
        /// Handles multi-select drag. Moves all selected root elements and returns whether
        /// the dragged element should skip moving itself (because a selected ancestor will move it).
        /// </summary>
        private bool HandleMultiSelectDrag(string draggedElementId, double deltaX, double deltaY)
        {
            // Find the dragged element
            var draggedElement = _selectedElements.FirstOrDefault(e => e.Id == draggedElementId);
            if (draggedElement == null) return false;

            // Check if dragged element has a selected ancestor - if so, it should NOT move itself
            bool hasSelectedAncestor = HasSelectedAncestor(draggedElement);

            // Only do multi-select move if we have multiple root selections
            if (_selectedRootElements.Count > 1 || hasSelectedAncestor)
            {
                // Track start positions on first move for undo support
                if (!_isMultiSelectDragActive)
                {
                    _isMultiSelectDragActive = true;
                    _multiSelectDragStartPositions.Clear();
                    foreach (var element in _selectedRootElements)
                    {
                        _multiSelectDragStartPositions[element.Id] = (element.X, element.Y);
                    }
                }

                // Move all selected ROOT elements
                foreach (var element in _selectedRootElements)
                {
                    element.X += deltaX;
                    element.Y += deltaY;
                }

                // Return true if dragged element has a selected ancestor (don't move itself)
                return hasSelectedAncestor;
            }

            // Single selection - element moves itself normally
            return false;
        }

        /// <summary>
        /// Gets the center snap position for an element during drag.
        /// Returns the snapped position and whether snap was applied.
        /// </summary>
        private (double x, double y, bool snapH, bool snapV) GetCenterSnapPosition(Rect elementRect)
        {
            var (snapH, snapV) = SpacingGuideOverlay.GetCenterSnapPosition(elementRect, out double snappedX, out double snappedY);
            return (snappedX, snappedY, snapH, snapV);
        }

        /// <summary>
        /// Check if element has an ancestor that is selected
        /// </summary>
        private bool HasSelectedAncestor(UIElementViewModel element)
        {
            var parent = element.Parent;
            while (parent != null && !parent.IsSystemElement)
            {
                if (_selectedElements.Contains(parent))
                    return true;
                parent = parent.Parent;
            }
            return false;
        }

        private IEnumerable<UIElementViewModel> GetAllNonSystemElements()
        {
            var result = new List<UIElementViewModel>();
            CollectNonSystemElements(RootElements, result);
            return result;
        }

        private void CollectNonSystemElements(IEnumerable<UIElementViewModel> elements, List<UIElementViewModel> result)
        {
            foreach (var element in elements)
            {
                if (!element.IsSystemElement)
                {
                    result.Add(element);
                }
                CollectNonSystemElements(element.Children, result);
            }
        }

        #endregion

        // Selection methods moved to MainWindow.Selection.cs

        #region Property Panel

        private void UpdatePropertyPanel()
        {
            if (_selectedElements.Count == 0)
            {
                PropertiesPanel.LoadElement(null);
            }
            else if (_selectedElements.Count == 1)
            {
                PropertiesPanel.LoadElement(_selectedElements[0]);
            }
            else
            {
                // Multiple selection - show count, don't load properties
                PropertiesPanel.LoadElement(null);
            }
        }

        /// <summary>
        /// Handles committed property changes from PropertyPanel for undo support
        /// </summary>
        private void PropertiesPanel_PropertyChangeCommitted(object? sender, PropertyChangedUndoEventArgs e)
        {
            // Create a property change command
            var command = new PropertyChangeCommand(
                e.Element,
                e.PropertyName,
                e.OldValue,
                e.NewValue,
                SetElementProperty);

            // Record it (property was already applied by PropertyPanel)
            _undoRedoManager.RecordExecuted(command);
        }

        /// <summary>
        /// Sets a property on a UIElementViewModel by reflection
        /// </summary>
        private void SetElementProperty(UIElementViewModel element, string propertyName, object? value)
        {
            var property = typeof(UIElementViewModel).GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(element, value);
            }
        }

        #endregion

        // Drag and Drop handlers moved to MainWindow.DragDrop.cs

        // Context Menu handlers moved to MainWindow.ContextMenu.cs

        #region Alignment Tools

        private void AlignLeft_Click(object sender, RoutedEventArgs e)
        {
            var command = _alignmentService.AlignLeft(_selectedElements.ToList());
            if (command != null) _undoRedoManager.Execute(command);
        }

        private void AlignCenterH_Click(object sender, RoutedEventArgs e)
        {
            var command = _alignmentService.AlignCenterH(_selectedElements.ToList());
            if (command != null) _undoRedoManager.Execute(command);
        }

        private void AlignRight_Click(object sender, RoutedEventArgs e)
        {
            var command = _alignmentService.AlignRight(_selectedElements.ToList());
            if (command != null) _undoRedoManager.Execute(command);
        }

        private void AlignTop_Click(object sender, RoutedEventArgs e)
        {
            var command = _alignmentService.AlignTop(_selectedElements.ToList());
            if (command != null) _undoRedoManager.Execute(command);
        }

        private void AlignCenterV_Click(object sender, RoutedEventArgs e)
        {
            var command = _alignmentService.AlignCenterV(_selectedElements.ToList());
            if (command != null) _undoRedoManager.Execute(command);
        }

        private void AlignBottom_Click(object sender, RoutedEventArgs e)
        {
            var command = _alignmentService.AlignBottom(_selectedElements.ToList());
            if (command != null) _undoRedoManager.Execute(command);
        }

        private void DistributeH_Click(object sender, RoutedEventArgs e)
        {
            var command = _alignmentService.DistributeH(_selectedElements.ToList());
            if (command != null) _undoRedoManager.Execute(command);
        }

        private void DistributeV_Click(object sender, RoutedEventArgs e)
        {
            var command = _alignmentService.DistributeV(_selectedElements.ToList());
            if (command != null) _undoRedoManager.Execute(command);
        }

        #endregion

        #region Snapping

        private double GetSnapValue()
        {
            if (double.TryParse(SnapValueTextBox.Text, out double snap) && snap > 0)
                return snap;
            return 1; // Default to pixel snap
        }

        public double SnapToGrid(double value)
        {
            if (!_snapToGrid)
                return _snapToPixel ? Math.Round(value) : value;

            var snap = GetSnapValue();
            var snapped = Math.Round(value / snap) * snap;
            return _snapToPixel ? Math.Round(snapped) : snapped;
        }

        public Point SnapToGrid(Point point)
        {
            return new Point(SnapToGrid(point.X), SnapToGrid(point.Y));
        }

        #endregion

        // Zoom and Pan methods moved to MainWindow.ZoomPan.cs

        // Guides methods moved to MainWindow.Guides.cs
        // Page State methods moved to MainWindow.PageState.cs
        // Cut/Copy/Paste methods moved to MainWindow.Clipboard.cs

        #region Undo/Redo

        private void Undo()
        {
            _undoRedoManager.Undo();
            UpdatePropertyPanel();
        }

        private void Redo()
        {
            _undoRedoManager.Redo();
            UpdatePropertyPanel();
        }

        private void MenuEdit_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            bool hasSelection = _selectedElements.Count > 0;
            bool hasClipboard = _clipboardElements.Count > 0;

            MenuUndo.IsEnabled = _undoRedoManager.CanUndo;
            MenuRedo.IsEnabled = _undoRedoManager.CanRedo;
            MenuCut.IsEnabled = hasSelection;
            MenuCopy.IsEnabled = hasSelection;
            MenuPaste.IsEnabled = hasClipboard;
            MenuDuplicate.IsEnabled = hasSelection;
            MenuDelete.IsEnabled = hasSelection;
            MenuCreateParent.IsEnabled = hasSelection;
            MenuBringToFront.IsEnabled = hasSelection;
            MenuSendToBack.IsEnabled = hasSelection;
        }

        private void MenuUndo_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }

        private void MenuRedo_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }

        private void MenuCut_Click(object sender, RoutedEventArgs e)
        {
            CutElement();
        }

        private void MenuCopy_Click(object sender, RoutedEventArgs e)
        {
            CopyElement();
        }

        private void MenuPaste_Click(object sender, RoutedEventArgs e)
        {
            PasteElement();
        }

        private void RemoveElementVisual(UIElementViewModel element)
        {
            _renderService.RemoveElementVisual(element);
        }

        #endregion

        #region Project Content Panel

        private void AssetSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (AssetSearchBox.Text == "Search assets...")
            {
                AssetSearchBox.Text = "";
                AssetSearchBox.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void AssetSearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AssetSearchBox.Text))
            {
                AssetSearchBox.Text = "Search assets...";
                AssetSearchBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void AssetSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyAssetFilters();
        }

        private void ApplyAssetFilters()
        {
            if (_allProjectAssets == null) return;

            var searchText = AssetSearchBox.Text;
            bool hasSearchText = searchText != "Search assets..." && !string.IsNullOrWhiteSpace(searchText);

            IEnumerable<AssetItem> filtered = _allProjectAssets;

            // Apply folder filter first (uses Path which is relative from Assets folder)
            if (!string.IsNullOrEmpty(_currentFolderFilter))
            {
                // Asset paths are like "UI/Textures/image" - filter by folder path
                filtered = filtered.Where(a => a.Path.StartsWith(_currentFolderFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Then apply search filter (searches in name)
            if (hasSearchText)
            {
                filtered = filtered.Where(a => a.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            ProjectContentListView.ItemsSource = filtered.ToList();
        }

        private void FolderFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_connectedProject == null)
            {
                MessageBox.Show("No project connected. Connect to a project first.", "No Project", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Use the same AssetsPath as opening pages
            var assetsDir = _connectedProject.AssetsPath;

            // Open folder browser dialog
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select folder to filter assets",
                InitialDirectory = assetsDir
            };

            if (dialog.ShowDialog() == true)
            {
                var selectedPath = dialog.FolderName;

                // Make sure selected folder is within Assets
                if (!selectedPath.StartsWith(assetsDir, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Please select a folder within the Assets directory.", "Invalid Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get relative path from Assets folder (this matches how asset names are stored)
                var relativePath = selectedPath.Substring(assetsDir.Length).TrimStart('\\', '/');

                // Convert backslashes to forward slashes (asset paths use forward slashes)
                relativePath = relativePath.Replace('\\', '/');

                // If relativePath is empty (selected Assets folder itself), clear filter
                if (string.IsNullOrEmpty(relativePath))
                {
                    _currentFolderFilter = null;
                    FolderFilterLabel.Text = "All";
                    FolderFilterLabel.ToolTip = null;
                    ClearFolderFilterButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Store the full relative path for accurate filtering
                    _currentFolderFilter = relativePath;
                    FolderFilterLabel.Text = System.IO.Path.GetFileName(selectedPath);
                    FolderFilterLabel.ToolTip = relativePath;
                    ClearFolderFilterButton.Visibility = Visibility.Visible;
                }
                ApplyAssetFilters();
                SaveContentBrowserState();
            }
        }

        private void ClearFolderFilterButton_Click(object sender, RoutedEventArgs e)
        {
            _currentFolderFilter = null;
            FolderFilterLabel.Text = "All";
            FolderFilterLabel.ToolTip = null;
            ClearFolderFilterButton.Visibility = Visibility.Collapsed;
            ApplyAssetFilters();
            SaveContentBrowserState();
        }

        private void SaveContentBrowserState()
        {
            if (string.IsNullOrEmpty(_connectedProjectSlnPath)) return;

            var projectData = _editorDataService.GetOrCreateProjectData(_connectedProjectSlnPath);
            projectData.ContentBrowserFolderFilter = _currentFolderFilter;
            _editorDataService.SaveProjectData(projectData);
        }

        private void RestoreContentBrowserState()
        {
            if (string.IsNullOrEmpty(_connectedProjectSlnPath)) return;

            var projectData = _editorDataService.LoadProjectData(_connectedProjectSlnPath);
            if (projectData == null) return;

            _currentFolderFilter = projectData.ContentBrowserFolderFilter;

            if (!string.IsNullOrEmpty(_currentFolderFilter))
            {
                // Extract folder name from path for display
                var folderName = _currentFolderFilter.Contains('/')
                    ? _currentFolderFilter.Substring(_currentFolderFilter.LastIndexOf('/') + 1)
                    : _currentFolderFilter;
                FolderFilterLabel.Text = folderName;
                FolderFilterLabel.ToolTip = _currentFolderFilter;
                ClearFolderFilterButton.Visibility = Visibility.Visible;
            }
            else
            {
                FolderFilterLabel.Text = "All";
                FolderFilterLabel.ToolTip = null;
                ClearFolderFilterButton.Visibility = Visibility.Collapsed;
            }

            ApplyAssetFilters();
        }

        #endregion

        // File Menu handlers moved to MainWindow.FileMenu.cs

        #region View Menu Handlers

        private void MenuZoomIn_Click(object sender, RoutedEventArgs e)
        {
            Zoom(ZoomStep);
        }

        private void MenuZoomOut_Click(object sender, RoutedEventArgs e)
        {
            Zoom(-ZoomStep);
        }

        private void MenuResetZoom_Click(object sender, RoutedEventArgs e)
        {
            SetZoom(1.0);
        }

        private void MenuLoadRefImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*",
                Title = "Load Reference Image"
            };

            // Use last reference image path if available
            if (!string.IsNullOrEmpty(_settingsService.LastReferenceImagePath))
            {
                var lastDir = System.IO.Path.GetDirectoryName(_settingsService.LastReferenceImagePath);
                if (System.IO.Directory.Exists(lastDir))
                {
                    dialog.InitialDirectory = lastDir;
                }
            }

            if (dialog.ShowDialog() == true)
            {
                LoadReferenceImage(dialog.FileName, RefImageOpacitySlider.Value / 100.0);

                // Also save to global settings for initial directory next time
                _settingsService.LastReferenceImagePath = dialog.FileName;
            }
        }

        private void MenuClearRefImage_Click(object sender, RoutedEventArgs e)
        {
            ReferenceImage.Source = null;
            ReferenceImage.Visibility = Visibility.Collapsed;
            MenuClearRefImage.IsEnabled = false;
            _currentReferenceImagePath = null;
        }

        private void RefImageOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RefImageOpacityText != null)
            {
                RefImageOpacityText.Text = $"{(int)e.NewValue}%";
            }

            if (ReferenceImage != null)
            {
                ReferenceImage.Opacity = e.NewValue / 100.0;
                _settingsService.ReferenceImageOpacity = e.NewValue / 100.0;
            }
        }

        private void MenuKeyboardShortcuts_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Views.KeyboardShortcutsDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var versionString = $"{version?.Major}.{version?.Minor}.{version?.Build}";

            MessageBox.Show(
                $"HS Stride UI Editor\n" +
                $"Version {versionString}\n\n" +
                "A standalone visual UI editor for the Stride game engine.\n\n" +
                "CONTROLS:\n" +
                "Ctrl/Alt + Scroll: Zoom in/out\n" +
                "Shift + Scroll: Horizontal scroll\n" +
                "Middle Mouse / Space + Drag: Pan canvas\n" +
                "Shift + Drag: Maintain aspect ratio\n" +
                "Alt + Drag: Scale from center\n" +
                "Ctrl + Click: Multi-select\n" +
                "Delete: Delete selected\n" +
                "Ctrl+D: Duplicate selected\n" +
                "Ctrl+Z/Y: Undo/Redo\n\n" +
                "© 2025 Happenstance Games LLC\n" +
                "Licensed under the MIT License\n\n" +
                "Contact: Dave@happenstancegames.com",
                "About",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion

        #region Toolkit Integration - Project Content

        private void PopulateProjectContent()
        {
            if (!_assetService.IsProjectConnected) return;

            try
            {
                // Get assets from service
                var assets = _assetService.GetProjectAssets();

                // Store full list for search filtering
                _allProjectAssets = assets.ToList();

                // Show assets
                if (assets.Count > 0)
                {
                    EmptyStatePanel.Visibility = Visibility.Collapsed;
                    ProjectContentListView.Visibility = Visibility.Visible;
                    ProjectContentListView.ItemsSource = assets;
                }
                else
                {
                    MessageBox.Show("No texture or sprite assets found in project.",
                        "No Assets", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load project assets:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Toolkit Integration - UIPage Conversion

        #endregion

        #region Unsaved Changes Handling

        /// <summary>
        /// Check if there are unsaved changes and prompt user to save.
        /// Returns true if the operation should continue, false if cancelled.
        /// </summary>
        private bool CheckUnsavedChangesAndPrompt()
        {
            if (!_fileService.HasUnsavedChanges) return true;

            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save before continuing?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    // Save the document
                    MenuSave_Click(this, new RoutedEventArgs());
                    // If save was cancelled (e.g., SaveAs dialog cancelled), abort
                    return !_fileService.HasUnsavedChanges;

                case MessageBoxResult.No:
                    // Don't save, continue
                    return true;

                case MessageBoxResult.Cancel:
                default:
                    // Cancel the operation
                    return false;
            }
        }

        /// <summary>
        /// Mark the document as having unsaved changes
        /// </summary>
        private void MarkDocumentAsChanged()
        {
            if (!_isDocumentLoaded) return;

            _fileService.HasUnsavedChanges = true;
            UpdateTitleWithUnsavedIndicator();
        }

        /// <summary>
        /// Update the page name indicator to show asterisk if there are unsaved changes
        /// </summary>
        private void UpdateTitleWithUnsavedIndicator()
        {
            var fileName = !string.IsNullOrEmpty(_fileService.CurrentFilePath)
                ? System.IO.Path.GetFileNameWithoutExtension(_fileService.CurrentFilePath)
                : "New Page";

            var unsavedIndicator = _fileService.HasUnsavedChanges ? "*" : "";
            PageNameIndicator.Text = $"{fileName}{unsavedIndicator}";
        }

        /// <summary>
        /// Handle window closing - prompt to save unsaved changes and save window state
        /// </summary>
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!CheckUnsavedChangesAndPrompt())
            {
                e.Cancel = true;
                return;
            }

            // Save window state
            _settingsService.IsMaximized = WindowState == WindowState.Maximized;

            // Save window bounds (use RestoreBounds if maximized to get the normal size)
            if (WindowState == WindowState.Maximized)
            {
                _settingsService.WindowBounds = (RestoreBounds.Left, RestoreBounds.Top, RestoreBounds.Width, RestoreBounds.Height);
            }
            else
            {
                _settingsService.WindowBounds = (Left, Top, Width, Height);
            }
        }

        /// <summary>
        /// Restore window state from settings (position, size, maximized state)
        /// </summary>
        private void RestoreWindowState()
        {
            var bounds = _settingsService.WindowBounds;

            // Validate bounds are within screen area
            var screenWidth = SystemParameters.VirtualScreenWidth;
            var screenHeight = SystemParameters.VirtualScreenHeight;

            // Ensure window is visible on screen
            if (bounds.Left >= 0 && bounds.Left < screenWidth - 100 &&
                bounds.Top >= 0 && bounds.Top < screenHeight - 100 &&
                bounds.Width >= 400 && bounds.Height >= 300)
            {
                Left = bounds.Left;
                Top = bounds.Top;
                Width = bounds.Width;
                Height = bounds.Height;
            }

            // Restore maximized state
            if (_settingsService.IsMaximized)
            {
                WindowState = WindowState.Maximized;
            }
        }

        #endregion
    }
}

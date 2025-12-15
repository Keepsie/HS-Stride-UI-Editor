// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace HS.Stride.UI.Editor.Controls.Elements
{
    /// <summary>
    /// Factory for creating element handlers by type
    /// </summary>
    public static class ElementHandlerFactory
    {
        private static readonly Dictionary<string, IElementHandler> _handlers = new();
        private static readonly IElementHandler _defaultHandler = new Containers.CanvasHandler();

        static ElementHandlerFactory()
        {
            // Register all handlers
            RegisterHandler(new Containers.CanvasHandler());
            RegisterHandler(new Containers.GridHandler());
            RegisterHandler(new Containers.StackPanelHandler());
            RegisterHandler(new Containers.ScrollViewerHandler());
            RegisterHandler(new Containers.UniformGridHandler());

            RegisterHandler(new Controls.ButtonHandler());
            RegisterHandler(new Controls.ToggleButtonHandler());
            RegisterHandler(new Controls.EditTextHandler());
            RegisterHandler(new Controls.SliderHandler());
            RegisterHandler(new Controls.ScrollBarHandler());

            RegisterHandler(new Visual.ImageElementHandler());
            RegisterHandler(new Visual.TextBlockHandler());
            RegisterHandler(new Visual.ContentControlHandler());
            RegisterHandler(new Visual.ModalHandler());
        }

        private static void RegisterHandler(IElementHandler handler)
        {
            _handlers[handler.ElementType] = handler;
        }

        /// <summary>
        /// Gets the handler for the specified element type
        /// </summary>
        public static IElementHandler GetHandler(string elementType)
        {
            return _handlers.TryGetValue(elementType, out var handler)
                ? handler
                : _defaultHandler;
        }

        /// <summary>
        /// Gets all registered element types
        /// </summary>
        public static IEnumerable<string> GetAllElementTypes()
        {
            return _handlers.Keys;
        }

        /// <summary>
        /// Checks if a handler exists for the given element type
        /// </summary>
        public static bool HasHandler(string elementType)
        {
            return _handlers.ContainsKey(elementType);
        }
    }
}

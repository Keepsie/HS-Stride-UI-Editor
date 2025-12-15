// Copyright (c) 2025 Happenstance Games LLC
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.ComponentModel;

namespace HS.Stride.UI.Editor.ViewModels
{
    /// <summary>
    /// Interface for undoable commands
    /// </summary>
    public interface IUndoableCommand
    {
        /// <summary>
        /// Execute the command
        /// </summary>
        void Execute();

        /// <summary>
        /// Undo the command
        /// </summary>
        void Undo();

        /// <summary>
        /// Description for display in menu (e.g., "Create Button")
        /// </summary>
        string Description { get; }
    }

    /// <summary>
    /// Manages undo/redo stacks for the editor
    /// </summary>
    public class UndoRedoManager : INotifyPropertyChanged
    {
        private readonly Stack<IUndoableCommand> _undoStack = new();
        private readonly Stack<IUndoableCommand> _redoStack = new();
        private const int MaxStackSize = 100;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raised when a command is executed, undone, or redone (document changed)
        /// </summary>
        public event EventHandler? DocumentChanged;

        /// <summary>
        /// Whether there are commands to undo
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// Whether there are commands to redo
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// Description of the next undo action
        /// </summary>
        public string UndoDescription => _undoStack.Count > 0 ? $"Undo {_undoStack.Peek().Description}" : "Undo";

        /// <summary>
        /// Description of the next redo action
        /// </summary>
        public string RedoDescription => _redoStack.Count > 0 ? $"Redo {_redoStack.Peek().Description}" : "Redo";

        /// <summary>
        /// Execute a command and add it to the undo stack
        /// </summary>
        public void Execute(IUndoableCommand command)
        {
            command.Execute();
            AddToUndoStack(command);
            DocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Record a command that was already executed (e.g., from UI interactions)
        /// </summary>
        public void RecordExecuted(IUndoableCommand command)
        {
            AddToUndoStack(command);
            DocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void AddToUndoStack(IUndoableCommand command)
        {
            _undoStack.Push(command);
            _redoStack.Clear(); // Clear redo stack when new action is performed

            // Limit stack size
            if (_undoStack.Count > MaxStackSize)
            {
                var tempList = _undoStack.ToList();
                tempList.RemoveAt(tempList.Count - 1);
                _undoStack.Clear();
                for (int i = tempList.Count - 1; i >= 0; i--)
                {
                    _undoStack.Push(tempList[i]);
                }
            }

            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoDescription));
            OnPropertyChanged(nameof(RedoDescription));
        }

        /// <summary>
        /// Undo the last command
        /// </summary>
        public void Undo()
        {
            if (!CanUndo) return;

            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);

            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoDescription));
            OnPropertyChanged(nameof(RedoDescription));
            DocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Redo the last undone command
        /// </summary>
        public void Redo()
        {
            if (!CanRedo) return;

            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);

            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoDescription));
            OnPropertyChanged(nameof(RedoDescription));
            DocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Clear all undo/redo history
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();

            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoDescription));
            OnPropertyChanged(nameof(RedoDescription));
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

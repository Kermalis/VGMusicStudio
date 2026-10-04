using System;
using System.Collections.Generic;

namespace Sanford.Collections.Generic
{
    internal class UndoManager
    {
        private Stack<ICommand> undoStack = new Stack<ICommand>();

        private Stack<ICommand> redoStack = new Stack<ICommand>();

        #region Methods

        public void Execute(ICommand command)
        {
            command.Execute();

            undoStack.Push(command);
            redoStack.Clear();
        }

        /// <summary>
        /// Undoes the last operation.
        /// </summary>
        /// <returns>
        /// <b>true</b> if the last operation was undone, <b>false</b> if there
        /// are no more operations left to undo.
        /// </returns>
        public bool Undo()
        {
            #region Guard

            if(undoStack.Count == 0)
            {
                return false;
            }

            #endregion

            ICommand command = undoStack.Pop();

            command.Undo();

            redoStack.Push(command);

            return true;
        }

        /// <summary>
        /// Redoes the last operation.
        /// </summary>
        /// <returns>
        /// <b>true</b> if the last operation was redone, <b>false</b> if there
        /// are no more operations left to redo.
        /// </returns>
        public bool Redo()
        {
            #region Guard

            if(redoStack.Count == 0)
            {
                return false;
            }

            #endregion

            ICommand command = redoStack.Pop();

            command.Execute();

            undoStack.Push(command);

            return true;
        }

        /// <summary>
        /// Clears the undo/redo history.
        /// </summary>
        public void ClearHistory()
        {
            undoStack.Clear();
            redoStack.Clear();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The number of operations left to undo.
        /// </summary>
        public int UndoCount
        {
            get
            {
                return undoStack.Count;
            }
        }

        /// <summary>
        /// The number of operations left to redo.
        /// </summary>
        public int RedoCount
        {
            get
            {
                return redoStack.Count;
            }
        }

        #endregion
    }
}

#region License

/* Copyright (c) 2006 Leslie Sanford
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to 
 * deal in the Software without restriction, including without limitation the 
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
 * sell copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software. 
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 * THE SOFTWARE.
 */

#endregion

#region Contact

/*
 * Leslie Sanford
 * Email: jabberdabber@hotmail.com
 */

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sanford.Collections.Generic
{
    /// <summary>
    /// Represents a list with undo/redo functionality.
    /// </summary>
    /// <typeparam name="T">
    /// The type of elements in the list.
    /// </typeparam>
    public partial class UndoableList<T> : IList<T>
    {
        #region UndoableList<T> Members

        #region Fields

        private List<T> theList;

        private UndoManager undoManager = new UndoManager();

        #endregion

        #region Construction

        public UndoableList()
        {
            theList = new List<T>();
        }

        public UndoableList(IEnumerable<T> collection)
        {
            theList = new List<T>(collection);
        }

        public UndoableList(int capacity)
        {
            theList = new List<T>(capacity);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Undoes the last operation.
        /// </summary>
        /// <returns>
        /// <b>true</b> if the last operation was undone, <b>false</b> if there
        /// are no more operations left to undo.
        /// </returns>
        public bool Undo()
        {
            return undoManager.Undo();
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
            return undoManager.Redo();
        }

        /// <summary>
        /// Clears the undo/redo history.
        /// </summary>
        public void ClearHistory()
        {
            undoManager.ClearHistory();
        }

        #region List Wrappers

        public int BinarySearch(T item)
        {
            return theList.BinarySearch(item);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return theList.BinarySearch(item, comparer);
        }

        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            return theList.BinarySearch(index, count, item, comparer);
        }

        public bool Contains(T item)
        {
            return theList.Contains(item);
        }

        public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            return theList.ConvertAll<TOutput>(converter);
        }

        public bool Exists(Predicate<T> match)
        {
            return theList.Exists(match);
        }

        public T Find(Predicate<T> match)
        {
            return theList.Find(match);
        }

        public List<T> FindAll(Predicate<T> match)
        {
            return theList.FindAll(match);
        }

        public int FindIndex(Predicate<T> match)
        {
            return theList.FindIndex(match);
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return theList.FindIndex(startIndex, match);
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            return theList.FindIndex(startIndex, count, match);
        }

        public int FindLastIndex(Predicate<T> match)
        {
            return theList.FindLastIndex(match);
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return theList.FindLastIndex(startIndex, match);
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            return theList.FindLastIndex(startIndex, count, match);
        }

        public T FindLast(Predicate<T> match)
        {
            return theList.FindLast(match);
        }

        public int LastIndexOf(T item)
        {
            return theList.LastIndexOf(item);
        }

        public int LastIndexOf(T item, int index)
        {
            return theList.LastIndexOf(item, index);
        }

        public int LastIndexOf(T item, int index, int count)
        {
            return theList.LastIndexOf(item, index, count);
        }

        public bool TrueForAll(Predicate<T> match)
        {
            return theList.TrueForAll(match);
        }

        public T[] ToArray()
        {
            return theList.ToArray();
        }

        public void TrimExcess()
        {
            theList.TrimExcess();
        }

        public void AddRange(IEnumerable<T> collection)
        {
            InsertRangeCommand command = new InsertRangeCommand(theList, theList.Count, collection);

            undoManager.Execute(command);
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            InsertRangeCommand command = new InsertRangeCommand(theList, index, collection);

            undoManager.Execute(command);
        }

        public void RemoveRange(int index, int count)
        {
            RemoveRangeCommand command = new RemoveRangeCommand(theList, index, count);

            undoManager.Execute(command);
        }

        public void Reverse()
        {
            ReverseCommand command = new ReverseCommand(theList);

            undoManager.Execute(command);
        }

        public void Reverse(int index, int count)
        {
            ReverseCommand command = new ReverseCommand(theList, index, count);

            undoManager.Execute(command);
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// The number of operations left to undo.
        /// </summary>
        public int UndoCount
        {
            get
            {
                return undoManager.UndoCount;
            }
        }

        /// <summary>
        /// The number of operations left to redo.
        /// </summary>
        public int RedoCount
        {
            get
            {
                return undoManager.RedoCount;
            }
        }

        #endregion

        #endregion

        #region IList<T> Members

        public int IndexOf(T item)
        {
            return theList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            InsertCommand command = new InsertCommand(theList, index, item);

            undoManager.Execute(command);
        }

        public void RemoveAt(int index)
        {
            RemoveAtCommand command = new RemoveAtCommand(theList, index);

            undoManager.Execute(command);
        }

        public T this[int index]
        {
            get
            {
                return theList[index];
            }
            set
            {
                SetCommand command = new SetCommand(theList, index, value);

                undoManager.Execute(command);
            }
        }

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            InsertCommand command = new InsertCommand(theList, Count, item);

            undoManager.Execute(command);
        }

        public void Clear()
        {
            #region Guard

            if(Count == 0)
            {
                return;
            }

            #endregion

            ClearCommand command = new ClearCommand(theList);

            undoManager.Execute(command);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            theList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get 
            {
                return theList.Count;
            }
        }

        public bool IsReadOnly
        {
            get 
            { 
                return false; 
            }
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            bool result;

            if(index >= 0)
            {
                RemoveAtCommand command = new RemoveAtCommand(theList, index);

                undoManager.Execute(command);

                result = true;
            }
            else
            {
                result = false;
            }

            return result;
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return theList.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return theList.GetEnumerator();
        }

        #endregion
    }
}
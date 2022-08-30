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

        /// <summary>
        /// The undoable construction list.
        /// </summary>
        public UndoableList()
        {
            theList = new List<T>();
        }

        /// <summary>
        /// The collection list of undoables.
        /// </summary>
        public UndoableList(IEnumerable<T> collection)
        {
            theList = new List<T>(collection);
        }

        /// <summary>
        /// The capacity list of undoables.
        /// </summary>
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

        /// <summary>
        /// Searches the entire list for an element using the default comparer.
        /// </summary>
        public int BinarySearch(T item)
        {
            return theList.BinarySearch(item);
        }

        /// <summary>
        /// Searches the entire list for an element using a specified comparer.
        /// </summary>
        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return theList.BinarySearch(item, comparer);
        }

        /// <summary>
        /// Searches a range of elements in the sorted list for an element using a specified comparer.
        /// </summary>
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            return theList.BinarySearch(index, count, item, comparer);
        }

        /// <summary>
        /// Determines whenever the list contains the undo/redo option.
        /// </summary>
        public bool Contains(T item)
        {
            return theList.Contains(item);
        }

        /// <summary>
        /// Converts all the data that is being read into the option chosen.
        /// </summary>
        public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            return theList.ConvertAll<TOutput>(converter);
        }

        /// <summary>
        /// If the data exists and matches, it returns the value as true.
        /// </summary>
        public bool Exists(Predicate<T> match)
        {
            return theList.Exists(match);
        }

        /// <summary>
        /// Initiates trying to find the data that matches.
        /// </summary>
        public T Find(Predicate<T> match)
        {
            return theList.Find(match);
        }

        /// <summary>
        /// Initiates trying to find all the data results that match.
        /// </summary>
        public List<T> FindAll(Predicate<T> match)
        {
            return theList.FindAll(match);
        }

        /// <summary>
        /// Finds the index to the data.
        /// </summary>
        public int FindIndex(Predicate<T> match)
        {
            return theList.FindIndex(match);
        }

        /// <summary>
        /// Finds the index to the data based on the start of the index.
        /// </summary>
        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return theList.FindIndex(startIndex, match);
        }

        /// <summary>
        /// Finds the index to the data based on the start of the index and the count.
        /// </summary>
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            return theList.FindIndex(startIndex, count, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the T in Predicate, then returns the zero-based index of the last occurrence that matches the data if found, otherwise will be -1.
        /// </summary>
        public int FindLastIndex(Predicate<T> match)
        {
            return theList.FindLastIndex(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the T in Predicate and extended from the start of the index, then returns the zero-based index of the last occurrence that matches the data if found, otherwise will be -1.
        /// </summary>
        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return theList.FindLastIndex(startIndex, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the T in Predicate and extended from the start of the index and to show a specific number of options, then returns the zero-based index of the last occurrence that matches the data if found, otherwise will be -1.
        /// </summary>
        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            return theList.FindLastIndex(startIndex, count, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by T, then returns the last element that matches, otherwise it will be T by default.
        /// </summary>
        public T FindLast(Predicate<T> match)
        {
            return theList.FindLast(match);
        }

        /// <summary>
        /// Searches for the item, then returns the last occurrence of the item in the list.
        /// </summary>
        public int LastIndexOf(T item)
        {
            return theList.LastIndexOf(item);
        }

        /// <summary>
        /// Searches for the item, then returns the last occurrence of the item within the range of elements in the list.
        /// </summary>
        public int LastIndexOf(T item, int index)
        {
            return theList.LastIndexOf(item, index);
        }

        /// <summary>
        /// Searches for the item, then returns the last occurrence of the item within the range of elements in the list and contains a specified number of elements and ends at the specific index.
        /// </summary>
        public int LastIndexOf(T item, int index, int count)
        {
            return theList.LastIndexOf(item, index, count);
        }

        /// <summary>
        /// Determines whenever every element in the list matches the conditions set by the Predicate.
        /// </summary>
        public bool TrueForAll(Predicate<T> match)
        {
            return theList.TrueForAll(match);
        }

        /// <summary>
        /// Copies the elements of the list to a new array.
        /// </summary>
        public T[] ToArray()
        {
            return theList.ToArray();
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the list, if the number is less than a threshold value.
        /// </summary>
        public void TrimExcess()
        {
            theList.TrimExcess();
        }

        /// <summary>
        /// Adds a range of elements to insert from the list with the number of elements.
        /// </summary>
        public void AddRange(IEnumerable<T> collection)
        {
            InsertRangeCommand command = new InsertRangeCommand(theList, theList.Count, collection);

            undoManager.Execute(command);
        }

        /// <summary>
        /// Inserts the range of elements from the list index into the undo/redo manager.
        /// </summary>
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            InsertRangeCommand command = new InsertRangeCommand(theList, index, collection);

            undoManager.Execute(command);
        }

        /// <summary>
        /// Removes a range of elements to insert from the list with the number of elements.
        /// </summary>
        public void RemoveRange(int index, int count)
        {
            RemoveRangeCommand command = new RemoveRangeCommand(theList, index, count);

            undoManager.Execute(command);
        }

        /// <summary>
        /// Reverts any added element or any removed element from the list.
        /// </summary>
        public void Reverse()
        {
            ReverseCommand command = new ReverseCommand(theList);

            undoManager.Execute(command);
        }

        /// <summary>
        /// Reverts any added element or any removed element from the list and shows the number of elements.
        /// </summary>
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

        /// <summary>
        /// Searches for a list of undo/redo functions via an index.
        /// </summary>
        public int IndexOf(T item)
        {
            return theList.IndexOf(item);
        }

        /// <summary>
        /// Inserts the undo/redo listed options from the index.
        /// </summary>
        public void Insert(int index, T item)
        {
            InsertCommand command = new InsertCommand(theList, index, item);

            undoManager.Execute(command);
        }

        /// <summary>
        /// Allows to remove the undo/redo options listed by command.
        /// </summary>
        public void RemoveAt(int index)
        {
            RemoveAtCommand command = new RemoveAtCommand(theList, index);

            undoManager.Execute(command);
        }

        /// <summary>
        /// Gets or sets the undo/redo options from the list.
        /// </summary>
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

        /// <summary>
        /// Adds an undo/redo option to the list of undo/redo options.
        /// </summary>
        public void Add(T item)
        {
            InsertCommand command = new InsertCommand(theList, Count, item);

            undoManager.Execute(command);
        }

        /// <summary>
        /// Clears an undo/redo option from the list of undo/redo options.
        /// </summary>
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

        /// <summary>
        /// Copies an undo/redo option from the list to an array.
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            theList.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Counts the list of undo/redo options from the list.
        /// </summary>
        public int Count
        {
            get 
            {
                return theList.Count;
            }
        }

        /// <summary>
        /// Checks if the list is read only, and returns if it is false.
        /// </summary>
        public bool IsReadOnly
        {
            get 
            { 
                return false; 
            }
        }

        /// <summary>
        /// Removes an undo/redo option from the list.
        /// </summary>
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

        /// <summary>
        /// Gets an enumerator and returns an enumerator that iterates through the list.
        /// </summary>
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
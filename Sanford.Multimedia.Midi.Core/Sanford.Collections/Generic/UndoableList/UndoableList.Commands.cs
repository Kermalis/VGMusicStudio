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
    public partial class UndoableList<T> : IList<T>
    {

        #region SetCommand

        private class SetCommand : ICommand
        {
            private IList<T> theList;

            private int index;

            private T oldItem;

            private T newItem;

            private bool undone = true;

            public SetCommand(IList<T> theList, int index, T item)
            {
                this.theList = theList;
                this.index = index;
                this.newItem = item;
            }

            #region ICommand Members

            public void Execute()
            {
                #region Guard

                if(!undone)
                {
                    return;
                }

                #endregion

                Debug.Assert(index >= 0 && index < theList.Count);

                oldItem = theList[index];
                theList[index] = newItem;
                undone = false;
            }

            public void Undo()
            {
                #region Guard

                if(undone)
                {
                    return;
                }

                #endregion

                Debug.Assert(index >= 0 && index < theList.Count);
                Debug.Assert(theList[index].Equals(newItem));

                theList[index] = oldItem;
                undone = true;
            }

            #endregion
        }

        #endregion

        #region InsertCommand

        private class InsertCommand : ICommand
        {
            private IList<T> theList;

            private int index;

            private T item;

            private bool undone = true;

            private int count;

            public InsertCommand(IList<T> theList, int index, T item)
            {
                this.theList = theList;
                this.index = index;
                this.item = item;
            }

            #region ICommand Members

            public void Execute()
            {
                #region Guard

                if(!undone)
                {
                    return;
                }

                #endregion

                Debug.Assert(index >= 0 && index <= theList.Count);

                count = theList.Count;
                theList.Insert(index, item);                
                undone = false;                
            }

            public void Undo()
            {
                #region Guard

                if(undone)
                {
                    return;
                }

                #endregion

                Debug.Assert(index >= 0 && index <= theList.Count);
                Debug.Assert(theList[index].Equals(item));

                theList.RemoveAt(index);
                undone = true;

                Debug.Assert(theList.Count == count);
            }

            #endregion
        }

        #endregion

        #region InsertRangeCommand

        private class InsertRangeCommand : ICommand
        {
            private List<T> theList;

            private int index;

            private List<T> insertList;

            private bool undone = true;

            public InsertRangeCommand(List<T> theList, int index, IEnumerable<T> collection)
            {
                this.theList = theList;
                this.index = index;

                insertList = new List<T>(collection);
            }

            #region ICommand Members

            public void Execute()
            {
                #region Guard

                if(!undone)
                {
                    return;
                }

                #endregion

                Debug.Assert(index >= 0 && index <= theList.Count);

                theList.InsertRange(index, insertList);

                undone = false;
            }

            public void Undo()
            {
                #region Guard

                if(undone)
                {
                    return;
                }

                #endregion

                Debug.Assert(index >= 0 && index <= theList.Count);

                theList.RemoveRange(index, insertList.Count);
                
                undone = true;
            }

            #endregion
        }

        #endregion

        #region RemoveAtCommand

        private class RemoveAtCommand : ICommand        
        {
            private IList<T> theList;

            private int index;

            private T item;

            private bool undone = true;

            private int count;

            public RemoveAtCommand(IList<T> theList, int index)
            {
                this.theList = theList;
                this.index = index;
            }

            #region ICommand Members

            public void Execute()
            {
                #region Guard

                if(!undone)
                {
                    return;
                }

                #endregion

                Debug.Assert(index >= 0 && index < theList.Count);

                item = theList[index];
                count = theList.Count;
                theList.RemoveAt(index);
                undone = false;
            }

            public void Undo()
            {
                #region Guard

                if(undone)
                {
                    return;
                }

                #endregion

                Debug.Assert(index >= 0 && index < theList.Count);

                theList.Insert(index, item);
                undone = true;

                Debug.Assert(theList.Count == count);
            }

            #endregion
        }

        #endregion

        #region RemoveRangeCommand

        private class RemoveRangeCommand : ICommand
        {
            private List<T> theList;

            private int index;

            private int count;

            private List<T> rangeList = new List<T>();

            private bool undone = true;

            public RemoveRangeCommand(List<T> theList, int index, int count)
            {
                this.theList = theList;
                this.index = index;
                this.count = count;
            }

            #region ICommand Members

            public void Execute()
            {
                #region Guard

                if(!undone)
                {
                    return;
                }

                #endregion

                Debug.Assert(index >= 0 && index < theList.Count);
                Debug.Assert(index + count <= theList.Count);

                rangeList = new List<T>(theList.GetRange(index, count));

                theList.RemoveRange(index, count);

                undone = false;
            }

            public void Undo()
            {
                #region Guard

                if(undone)
                {
                    return;
                }

                #endregion

                theList.InsertRange(index, rangeList);

                undone = true;
            }

            #endregion
        }

        #endregion

        #region ClearCommand

        private class ClearCommand : ICommand
        {
            private IList<T> theList;

            private IList<T> undoList;

            private bool undone = true;

            public ClearCommand(IList<T> theList)
            {
                this.theList = theList;
            }

            #region ICommand Members

            public void Execute()
            {
                #region Guard

                if(!undone)
                {
                    return;
                }

                #endregion

                undoList = new List<T>(theList);

                theList.Clear();

                undone = false;
            }

            public void Undo()
            {
                #region Guard

                if(undone)
                {
                    return;
                }

                #endregion

                Debug.Assert(theList.Count == 0);

                foreach(T item in undoList)
                {
                    theList.Add(item);
                }

                undoList.Clear();

                undone = true;
            }

            #endregion
        }

        #endregion

        #region ReverseCommand

        private class ReverseCommand : ICommand
        {
            private List<T> theList;

            private int index;

            private int count;

            private bool reverseRange;

            private bool undone = true;

            public ReverseCommand(List<T> theList)
            {
                this.theList = theList;
                this.reverseRange = false;
            }

            public ReverseCommand(List<T> theList, int index, int count)
            {
                this.theList = theList;
                this.index = index;
                this.count = count;
                this.reverseRange = true;
            }

            #region ICommand Members

            public void Execute()
            {
                #region Guard

                if(!undone)
                {
                    return;
                }

                #endregion

                if(reverseRange)
                {
                    theList.Reverse(index, count);
                }
                else
                {
                    theList.Reverse();
                }

                undone = false;
            }

            public void Undo()
            {
                #region Guard

                if(undone)
                {
                    return;
                }

                #endregion

                if(reverseRange)
                {
                    theList.Reverse(index, count);
                }
                else
                {
                    theList.Reverse();
                }

                undone = true;
            }

            #endregion
        }

        #endregion
    }
}
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
        [Conditional("DEBUG")]
        public static void Test()
        {
            int count = 10;
            List<int> comparisonList = new List<int>(count);
            UndoableList<int> undoList = new UndoableList<int>(count);

            PopulateLists(comparisonList, undoList, count);
            TestAdd(comparisonList, undoList);
            TestClear(comparisonList, undoList);
            TestInsert(comparisonList, undoList);
            TestInsertRange(comparisonList, undoList);
            TestRemove(comparisonList, undoList);
            TestRemoveAt(comparisonList, undoList);
            TestRemoveRange(comparisonList, undoList);
            TestReverse(comparisonList, undoList);
        }

        [Conditional("DEBUG")]
        private static void TestAdd(List<int> comparisonList, UndoableList<int> undoList)
        {
            TestEquals(comparisonList, undoList);

            Stack<int> redoStack = new Stack<int>();

            while(comparisonList.Count > 0)
            {
                redoStack.Push(comparisonList[comparisonList.Count - 1]);
                comparisonList.RemoveAt(comparisonList.Count - 1);
                Debug.Assert(undoList.Undo());
                TestEquals(comparisonList, undoList);
            }

            while(redoStack.Count > 0)
            {
                comparisonList.Add(redoStack.Pop());
                Debug.Assert(undoList.Redo());
                TestEquals(comparisonList, undoList);
            }
        }

        [Conditional("DEBUG")]
        private static void TestClear(List<int> comparisonList, UndoableList<int> undoList)
        {
            TestEquals(comparisonList, undoList);

            undoList.Clear();

            Debug.Assert(undoList.Undo());

            TestEquals(comparisonList, undoList);
        }

        [Conditional("DEBUG")]
        private static void TestInsert(List<int> comparisonList, UndoableList<int> undoList)
        {
            TestEquals(comparisonList, undoList);

            int index = comparisonList.Count / 2;

            comparisonList.Insert(index, 999);
            undoList.Insert(index, 999);

            comparisonList.RemoveAt(index);
            Debug.Assert(undoList.Undo());

            TestEquals(comparisonList, undoList);

            comparisonList.Insert(index, 999);
            Debug.Assert(undoList.Redo());

            TestEquals(comparisonList, undoList);
        }

        [Conditional("DEBUG")]
        private static void TestInsertRange(List<int> comparisonList, UndoableList<int> undoList)
        {
            TestEquals(comparisonList, undoList);

            int[] range = { 1, 2, 3, 4, 5 };
            int index = comparisonList.Count / 2;

            comparisonList.InsertRange(index, range);
            undoList.InsertRange(index, range);

            TestEquals(comparisonList, undoList);

            comparisonList.RemoveRange(index, range.Length);
            Debug.Assert(undoList.Undo());
            
            TestEquals(comparisonList, undoList);

            comparisonList.InsertRange(index, range);
            Debug.Assert(undoList.Redo());

            TestEquals(comparisonList, undoList);
        }

        [Conditional("DEBUG")]
        private static void TestRemove(List<int> comparisonList, UndoableList<int> undoList)
        {
            TestEquals(comparisonList, undoList);

            int index = comparisonList.Count / 2;

            int item = comparisonList[index];

            comparisonList.Remove(item);
            undoList.Remove(item);

            TestEquals(comparisonList, undoList);

            comparisonList.Insert(index, item);
            Debug.Assert(undoList.Undo());

            TestEquals(comparisonList, undoList);
        }

        [Conditional("DEBUG")]
        private static void TestRemoveAt(List<int> comparisonList, UndoableList<int> undoList)
        {
            TestEquals(comparisonList, undoList);

            int index = comparisonList.Count / 2;

            int item = comparisonList[index];

            comparisonList.RemoveAt(index);
            undoList.RemoveAt(index);

            TestEquals(comparisonList, undoList);

            comparisonList.Insert(index, item);
            Debug.Assert(undoList.Undo());

            TestEquals(comparisonList, undoList);
        }

        [Conditional("DEBUG")]
        private static void TestRemoveRange(List<int> comparisonList, UndoableList<int> undoList)
        {
            TestEquals(comparisonList, undoList);

            int index = comparisonList.Count / 2;
            int count = comparisonList.Count - index;

            List<int> range = comparisonList.GetRange(index, count);

            comparisonList.RemoveRange(index, count);
            undoList.RemoveRange(index, count);

            TestEquals(comparisonList, undoList);

            comparisonList.InsertRange(index, range);
            Debug.Assert(undoList.Undo());

            TestEquals(comparisonList, undoList);
        }

        [Conditional("DEBUG")]
        private static void TestReverse(List<int> comparisonList, UndoableList<int> undoList)
        {
            TestEquals(comparisonList, undoList);

            comparisonList.Reverse();
            undoList.Reverse();

            TestEquals(comparisonList, undoList);

            comparisonList.Reverse();
            Debug.Assert(undoList.Undo());

            TestEquals(comparisonList, undoList);

            comparisonList.Reverse();
            Debug.Assert(undoList.Redo());

            TestEquals(comparisonList, undoList);

            int count = comparisonList.Count / 2;

            comparisonList.Reverse(0, count);
            undoList.Reverse(0, count);

            TestEquals(comparisonList, undoList);

            comparisonList.Reverse(0, count);
            Debug.Assert(undoList.Undo());

            TestEquals(comparisonList, undoList);

            comparisonList.Reverse(0, count);
            Debug.Assert(undoList.Redo());

            TestEquals(comparisonList, undoList);
        }

        [Conditional("DEBUG")]
        private static void PopulateLists(IList<int> a, IList<int> b, int count)
        {
            Random r = new Random();
            int item;

            for(int i = 0; i < count; i++)
            {
                item = r.Next();
                a.Add(item);
                b.Add(item);
            }
        }

        [Conditional("DEBUG")]
        private static void TestEquals(ICollection<int> a, ICollection<int> b)
        {
            bool equals = true;

            if(a.Count != b.Count)
            {
                equals = false;
            }
            IEnumerator<int> aEnumerator = a.GetEnumerator();
            IEnumerator<int> bEnumerator = b.GetEnumerator();

            while(equals && aEnumerator.MoveNext() && bEnumerator.MoveNext())
            {
                equals = aEnumerator.Current.Equals(bEnumerator.Current);
            }

            Debug.Assert(equals);
        }
    }
}
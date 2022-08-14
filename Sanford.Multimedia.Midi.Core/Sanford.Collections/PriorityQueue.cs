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
using System.Collections;
using System.Diagnostics;

namespace Sanford.Collections
{
	/// <summary>
	/// Represents the priority queue data structure.
	/// </summary>
	public class PriorityQueue : ICollection
    {
        #region PriorityQueue Members

        #region Fields

        // The maximum level of the skip list.
        private const int LevelMaxValue = 16;

        // The probability value used to randomly select the next level value.
        private const double Probability = 0.5;

        // The current level of the skip list.
        private int currentLevel = 1;

        // The header node of the skip list.
        private Node header = new Node(null, LevelMaxValue);

        // Used to generate node levels.
        private Random rand = new Random();

        // The number of elements in the PriorityQueue.
        private int count = 0;

        // The version of this PriorityQueue.
        private long version = 0;

        // Used for comparing and sorting elements.
        private IComparer comparer;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the PriorityQueue class.
        /// </summary>
        /// <remarks>
        /// The PriorityQueue will cast its elements to the IComparable 
        /// interface when making comparisons.
        /// </remarks>
        public PriorityQueue()
        {
            comparer = new DefaultComparer();
        }

        /// <summary>
        /// Initializes a new instance of the PriorityQueue class with the
        /// specified IComparer.
        /// </summary>
        /// <param name="comparer">
        /// The IComparer to use for comparing and ordering elements.
        /// </param>
        /// <remarks>
        /// If the specified IComparer is null, the PriorityQueue will cast its
        /// elements to the IComparable interface when making comparisons.
        /// </remarks>
        public PriorityQueue(IComparer comparer)
        {
            // If no comparer was provided.
            if(comparer == null)
            {
                // Use the DefaultComparer.
                this.comparer = new DefaultComparer();
            }
            // Else a comparer was provided.
            else
            {
                // Use the provided comparer.
                this.comparer = comparer;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Enqueues the specified element into the PriorityQueue.
        /// </summary>
        /// <param name="element">
        /// The element to enqueue into the PriorityQueue.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If element is null.
        /// </exception>
        public virtual void Enqueue(object element)
        {
            #region Require

            if(element == null)
            {
                throw new ArgumentNullException("element");
            }

            #endregion

            Node x = header;
            Node[] update = new Node[LevelMaxValue];
            int nextLevel = NextLevel();

            // Find the place in the queue to insert the new element.
            for(int i = currentLevel - 1; i >= 0; i--)
            {
                while(x[i] != null && comparer.Compare(x[i].Element, element) > 0)
                {
                    x = x[i];
                }

                update[i] = x;                                
            }     
            
            // If the new node's level is greater than the current level.
            if(nextLevel > currentLevel)
            {
                for(int i = currentLevel; i < nextLevel; i++)
                {
                    update[i] = header;
                }

                // Update level.
                currentLevel = nextLevel;
            }

            // Create new node.
            Node newNode = new Node(element, nextLevel);

            // Insert the new node into the list.
            for(int i = 0; i < nextLevel; i++)
            {
                newNode[i] = update[i][i];
                update[i][i] = newNode;
            }

            // Keep track of the number of elements in the PriorityQueue.
            count++;

            version++;

            #region Ensure

            Debug.Assert(Contains(element), "Contains Test", "Contains test for element " + element.ToString() + " failed.");

            #endregion

            #region Invariant

            AssertValid();

            #endregion
        }

        /// <summary>
        /// Removes the element at the head of the PriorityQueue.
        /// </summary>
        /// <returns>
        /// The element at the head of the PriorityQueue.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// If Count is zero.
        /// </exception>
        public virtual object Dequeue()
        {
            #region Require

            if(Count == 0)
            {
                throw new InvalidOperationException(
                    "Cannot dequeue into an empty PriorityQueue.");
            }

            #endregion

            // Get the first item in the queue.
            object element = header[0].Element;

            // Keep track of the node that is about to be removed.
            Node oldNode = header[0];

            // Update the header so that its pointers that pointed to the
            // node to be removed now point to the node that comes after it.
            for(int i = 0; i < currentLevel && header[i] == oldNode; i++)
            {
                header[i] = oldNode[i];
            }

            // Update the current level of the list in case the node that
            // was removed had the highest level.
            while(currentLevel > 1 && header[currentLevel - 1] == null)
            {
                currentLevel--;
            }

            // Keep track of how many items are in the queue.
            count--;

            version++;

            #region Ensure

            Debug.Assert(count >= 0);

            #endregion

            #region Invariant

            AssertValid();

            #endregion

            return element;
        }

        /// <summary>
        /// Removes the specified element from the PriorityQueue.
        /// </summary>
        /// <param name="element">
        /// The element to remove.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If element is null
        /// </exception>
        public virtual void Remove(object element)
        {
            #region Require

            if(element == null)
            {
                throw new ArgumentNullException("element");
            }

            #endregion

            Node x = header;
            Node[] update = new Node[LevelMaxValue];
            int nextLevel = NextLevel();

            // Find the specified element.
            for(int i = currentLevel - 1; i >= 0; i--)
            {
                while(x[i] != null && comparer.Compare(x[i].Element, element) > 0)
                {
                    x = x[i];
                }

                update[i] = x;                                
            }  
   
            x = x[0];

            // If the specified element was found.
            if(x != null && comparer.Compare(x.Element, element) == 0)
            {
                // Remove element.
                for(int i = 0; i < currentLevel && update[i][i] == x; i++)
                {
                    update[i][i] = x[i];
                }

                // Update list level.
                while(currentLevel > 1 && header[currentLevel - 1] == null)
                {
                    currentLevel--;
                }

                // Keep track of the number of elements in the PriorityQueue.
                count--;

                version++;
            }

            #region Invariant

            AssertValid();

            #endregion
        }

        /// <summary>
        /// Returns a value indicating whether the specified element is in the
        /// PriorityQueue.
        /// </summary>
        /// <param name="element">
        /// The element to test.
        /// </param>
        /// <returns>
        /// <b>true</b> if the element is in the PriorityQueue; otherwise
        /// <b>false</b>.
        /// </returns>
        public virtual bool Contains(object element)
        {
            #region Guard

            if(element == null)
            {
                return false;
            }

            #endregion

            bool found;
            Node x = header;            

            // Find the specified element.
            for(int i = currentLevel - 1; i >= 0; i--)
            {
                while(x[i] != null && comparer.Compare(x[i].Element, element) > 0)
                {
                    x = x[i];
                }
            }

            x = x[0];

            // If the element is in the PriorityQueue.
            if(x != null && comparer.Compare(x.Element, element) == 0)
            {
                found = true;
            }
            // Else the element is not in the PriorityQueue.
            else
            {
                found = false;
            }

            return found;
        }

        /// <summary>
        /// Returns the element at the head of the PriorityQueue without 
        /// removing it.
        /// </summary>
        /// <returns>
        /// The element at the head of the PriorityQueue.
        /// </returns>
        public virtual object Peek()
        {
            #region Require

            if(Count == 0)
            {
                throw new InvalidOperationException(
                    "Cannot peek into an empty PriorityQueue.");
            }

            #endregion

            return header[0].Element;
        }

        /// <summary>
        /// Removes all elements from the PriorityQueue.
        /// </summary>
        public virtual void Clear()
        {
            header = new Node(null, LevelMaxValue);

            currentLevel = 1;

            count = 0;

            version++;

            #region Invariant

            AssertValid();

            #endregion
        }

        /// <summary>
        /// Returns a synchronized wrapper of the specified PriorityQueue.
        /// </summary>
        /// <param name="queue">
        /// The PriorityQueue to synchronize.
        /// </param>
        /// <returns>
        /// A synchronized PriorityQueue.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If queue is null.
        /// </exception>
        public static PriorityQueue Synchronized(PriorityQueue queue)
        {
            #region Require
            
            if(queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            #endregion

            return new SynchronizedPriorityQueue(queue);
        }

        // Generates a random level for the next node.
        private int NextLevel()
        {
            int nextLevel = 1;

            while(rand.NextDouble() < Probability && 
                nextLevel < LevelMaxValue && 
                nextLevel <= currentLevel)
            {
                nextLevel++;
            }

            return nextLevel;
        }

        // Makes sure none of the PriorityQueue's invariants have been violated.
        [Conditional("DEBUG")]
        private void AssertValid()
        {
            int n = 0;
            Node x = header[0];           

            while(x != null)
            {
                if(x[0] != null)
                {
                    Debug.Assert(comparer.Compare(x.Element, x[0].Element) >= 0, "Order test");
                }

                x = x[0];
                n++;
            }

            Debug.Assert(n == Count, "Count test.");

            for(int i = 1; i < currentLevel; i++)
            {
                Debug.Assert(header[i] != null, "Level non-null test.");
            }

            for(int i = currentLevel; i < LevelMaxValue; i++)
            {
                Debug.Assert(header[i] == null, "Level null test.");
            }
        }

        [Conditional("DEBUG")]
        public static void Test()
        {
            Random r = new Random();
            PriorityQueue queue = new PriorityQueue();
            int count = 1000;
            int element;

            for(int i = 0; i < count; i++)
            {
                element = r.Next();
                queue.Enqueue(element);

                Debug.Assert(queue.Contains(element), "Contains Test");
            }

            Debug.Assert(queue.Count == count, "Count Test");

            int previousElement = (int)queue.Peek();
            int peekElement;          

            for(int i = 0; i < count; i++)
            {
                peekElement = (int)queue.Peek();
                element = (int)queue.Dequeue();

                Debug.Assert(element == peekElement, "Peek Test");
                Debug.Assert(element <= previousElement, "Order Test");

                previousElement = element;
            }

            Debug.Assert(queue.Count == 0);
        }

        #endregion

        #region Private Classes

        #region SynchronizedPriorityQueue Class

        // A synchronized wrapper for the PriorityQueue class.
        private class SynchronizedPriorityQueue : PriorityQueue
        {
            private PriorityQueue queue;

            private object root;

            public SynchronizedPriorityQueue(PriorityQueue queue)
            {
                #region Require

                if(queue == null)
                {
                    throw new ArgumentNullException("queue");
                }
                
                #endregion

                this.queue = queue;

                root = queue.SyncRoot;
            }

            public override void Enqueue(object element)
            {
                lock(root)
                {
                    queue.Enqueue(element);
                }
            }

            public override object Dequeue()
            {
                lock(root)
                {
                    return queue.Dequeue();
                }
            }

            public override void Remove(object element)
            {
                lock(root)
                {
                    queue.Remove(element);
                }
            }

            public override void Clear()
            {
                lock(root)
                {
                    queue.Clear();
                }
            }

            public override bool Contains(object element)
            {
                lock(root)
                {
                    return queue.Contains(element);
                }
            }

            public override object Peek()
            {
                lock(root)
                {
                    return queue.Peek();
                }
            }

            public override void CopyTo(Array array, int index)
            {
                lock(root)
                {
                    queue.CopyTo(array, index);
                }
            }

            public override int Count
            {
                get
                {
                    lock(root)
                    {
                        return queue.Count;
                    }
                }
            }

            public override bool IsSynchronized
            {
                get
                {
                    return true;
                }
            }

            public override object SyncRoot
            {
                get
                {
                    return root;
                }
            }

            public override IEnumerator GetEnumerator()
            {
                lock(root)
                {
                    return queue.GetEnumerator();
                }
            }
        }

        #endregion

        #region DefaultComparer Class

        // The IComparer to use of no comparer was provided.
        private class DefaultComparer : IComparer
        {
            #region IComparer Members

            public int Compare(object x, object y)
            {
                #region Require

                if(!(y is IComparable))
                {
                    throw new ArgumentException(
                        "Item does not implement IComparable.");
                }

                #endregion

                IComparable a = x as IComparable;

                Debug.Assert(a != null);

                return a.CompareTo(y);
            }

            #endregion
        }

        #endregion

        #region Node Class

        // Represents a node in the list of nodes.
        private class Node
        {
            private Node[] forward;

            private object element;

            public Node(object element, int level)
            {
                this.forward = new Node[level];
                this.element = element;
            }

            public Node this[int index]            
            {
                get
                {
                    return forward[index];
                }
                set
                {
                    forward[index] = value;
                }
            }

            public object Element
            {
                get
                {
                    return element;
                }
            }
        }

        #endregion

        #region PriorityQueueEnumerator Class

        // Implements the IEnumerator interface for the PriorityQueue class.
        private class PriorityQueueEnumerator : IEnumerator
        {
            private PriorityQueue owner;

            private Node head;

            private Node currentNode;

            private bool moveResult;

            private long version;

            public PriorityQueueEnumerator(PriorityQueue owner)
            {
                this.owner = owner;
                this.version = owner.version;
                head = owner.header;

                Reset();
            }

            #region IEnumerator Members

            public void Reset()
            {     
                #region Require

                if(version != owner.version)
                {
                    throw new InvalidOperationException(
                        "The PriorityQueue was modified after the enumerator was created.");
                }

                #endregion

                currentNode = head;  
                moveResult = true;
            }

            public object Current
            {
                get
                {
                    #region Require

                    if(currentNode == head || currentNode == null)
                    {
                        throw new InvalidOperationException(
                            "The enumerator is positioned before the first " +
                            "element of the collection or after the last element.");
                    }

                    #endregion

                    return currentNode.Element;
                }
            }

            public bool MoveNext()
            {
                #region Require

                if(version != owner.version)
                {
                    throw new InvalidOperationException(
                        "The PriorityQueue was modified after the enumerator was created.");
                }

                #endregion

                if(moveResult)
                {
                    currentNode = currentNode[0];
                }

                if(currentNode == null)
                {
                    moveResult = false;
                }

                return moveResult;                
            }

            #endregion
        }

        #endregion

        #endregion

        #endregion

        #region ICollection Members

        public virtual bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public virtual int Count
        {
            get
            {
                return count;
            }
        }

        public virtual void CopyTo(Array array, int index)
        {
            #region Require

            if(array == null)
            {
                throw new ArgumentNullException("array");
            }
            else if(index < 0)
            {
                throw new ArgumentOutOfRangeException("index", index,
                    "Array index out of range.");
            }
            else if(array.Rank > 1)
            {
                throw new ArgumentException(
                    "Array has more than one dimension.", "array");
            }
            else if(index >= array.Length)
            {
                throw new ArgumentException(
                    "index is equal to or greater than the length of array.", "index");
            }
            else if(Count > array.Length - index)
            {
                throw new ArgumentException(
                    "The number of elements in the PriorityQueue is greater " +
                    "than the available space from index to the end of the " +
                    "destination array.", "index");
            }

            #endregion

            int i = index;

            foreach(object element in this)
            {
                array.SetValue(element, i);
                i++;
            }
        }

        public virtual object SyncRoot
        {
            get
            {
                return this;
            }
        }

        #endregion

        #region IEnumerable Members

        public virtual IEnumerator GetEnumerator()
        {
            return new PriorityQueueEnumerator(this);
        }

        #endregion
    }
}

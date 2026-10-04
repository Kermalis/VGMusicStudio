/*
 * Created by: Leslie Sanford 
 * 
 * Last modified: 02/23/2005
 * 
 * Contact: jabberdabber@hotmail.com
 */

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;

namespace Sanford.Collections.Immutable
{
	/// <summary>
	/// Implements Chris Okasaki's random access list.
	/// </summary>
	[ImmutableObject(true)]
	public class RandomAccessList : IEnumerable
	{
        #region RandomAccessList Members

        #region Class Fields

        /// <summary>
        /// Represents an empty random access list.
        /// </summary> 
        public static readonly RandomAccessList Empty = new RandomAccessList();

        #endregion

        #region Instance Fields

        // The number of elements in the random access list.
        private readonly int count;

        // The first top node in the list.
        private readonly RalTopNode first;

        // A random access list representing the head of the current list.
        private RandomAccessList head = null;

        // A random access list representing the tail of the current list.
        private RandomAccessList tail = null;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the RandomAccessList class.
        /// </summary>
		public RandomAccessList()
		{
            count = 0;
            first = null;
		}

        /// <summary>
        /// Initializes a new instance of the RandomAccessList class with the
        /// specified first top node and the number of elements in the list.
        /// </summary>
        /// <param name="first">
        /// The first top node in the list.
        /// </param>
        /// <param name="count">
        /// The number of nodes in the list.
        /// </param>
        private RandomAccessList(RalTopNode first, int count)
        {
            this.first = first;
            this.count = count;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepends a value to the random access list.
        /// </summary>
        /// <param name="value">
        /// The value to prepend to the list.
        /// </param>
        /// <returns>
        /// A new random access list with the specified value prepended to the
        /// list.
        /// </returns>
        public RandomAccessList Cons(object value)
        {
            RandomAccessList result;

            // If the list is empty, or there is only one tree in the list, or
            // the first tree is smaller than the second tree.
            if(Count == 0 || 
                first.NextNode == null || 
                first.Root.Count < first.NextNode.Root.Count)
            {
                // Create a new first node with the specified value.
                RalTreeNode newRoot = new RalTreeNode(value, null, null);

                // Create a new random access list.
                result = new RandomAccessList(
                    new RalTopNode(newRoot, first), 
                    Count + 1);
            }
            // Else the first and second trees in the list are the same size.
            else
            {
                Debug.Assert(first.Root.Count == first.NextNode.Root.Count);

                // Create a new first node with the old first and second node 
                // as the left and right children respectively.
                RalTreeNode newRoot = new RalTreeNode(
                    value, 
                    first.Root, 
                    first.NextNode.Root);

                // Create a new random access list.
                result = new RandomAccessList(
                    new RalTopNode(newRoot, first.NextNode.NextNode), 
                    Count + 1);
            }

            return result;
        }

        /// <summary>
        /// Gets the value at the specified position in the current 
        /// RandomAccessList.
        /// </summary>
        /// <param name="index">
        /// An integer that represents the position of the RandomAccessList 
        /// element to get. 
        /// </param>
        /// <returns>
        /// The value at the specified position in the RandomAccessList.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// index is outside the range of valid indexes for the current 
        /// RandomAccessList.
        /// </exception>
        public object GetValue(int index)
        {
            // Precondition.
            if(index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException("index", index,
                    "Index out of range.");
            }

            return first.GetValue(index);
        }

        /// <summary>
        /// Sets the specified element in the current RandomAccessList to the 
        /// specified value.
        /// </summary>
        /// <param name="value">
        /// The new value for the specified element. 
        /// </param>
        /// <param name="index">
        /// An integer that represents the position of the RandomAccessList 
        /// element to set. 
        /// </param>
        /// <returns>
        /// A new RandomAccessList with the element at the specified position 
        /// set to the specified value.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// index is outside the range of valid indexes for the current 
        /// RandomAccessList.
        /// </exception>
        public RandomAccessList SetValue(object value, int index)
        {
            // Precondition.
            if(index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException("index", index,
                    "Index out of range.");
            }

            return new RandomAccessList(first.SetValue(value, index), Count);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the number of elements in the RandomAccessList.
        /// </summary>
        public int Count
        {
            get
            {
                return count;
            }
        }

        /// <summary>
        /// Gets a RandomAccessList with first element of the current 
        /// RandomAccessList.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// If the RandomAccessList is empty.
        /// </exception>
        public RandomAccessList Head
        {
            get
            {
                // Preconditions.
                if(Count == 0)
                {
                    throw new InvalidOperationException(
                        "Cannot get the head of an empty random access list.");
                }

                if(head == null)
                {
                    RalTreeNode newRoot = new RalTreeNode(
                        first.Root.Value, null, null);

                    RalTopNode newFirst = new RalTopNode(newRoot, null);

                    head = new RandomAccessList(newFirst, 1);
                }

                return head;
            }
        }

        /// <summary>
        /// Gets a RandomAccessList with all but the first element of the
        /// current RandomAccessList.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// If the RandomAccessList is empty.
        /// </exception>
        public RandomAccessList Tail
        {
            get
            {
                // Precondition.
                if(Count == 0)
                {
                    throw new InvalidOperationException(
                        "Cannot get the tail of an empty random access list.");
                }

                if(tail == null)
                {
                    if(Count == 1)
                    {
                        tail = Empty;
                    }
                    else
                    {                        
                        if(first.Root.Count > 1)
                        {
                            RalTreeNode left = first.Root.LeftChild;
                            RalTreeNode right = first.Root.RightChild;

                            RalTopNode newSecond = new RalTopNode(
                                right, first.NextNode);
                            RalTopNode newFirst = new RalTopNode(
                                left, newSecond);

                            tail = new RandomAccessList(newFirst, Count - 1);
                        }
                        else
                        {
                            tail = new RandomAccessList(first.NextNode, Count - 1);
                        }
                    }
                }

                return tail;
            }
        }

        #endregion

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an IEnumerator for the RandomAccessList.
        /// </summary>
        /// <returns>
        /// An IEnumerator for the RandomAccessList.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            return new RalEnumerator(first, Count);
        }

        #endregion
    }
}

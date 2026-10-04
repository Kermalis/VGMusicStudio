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

namespace Sanford.Collections.Immutable
{
	/// <summary>
	/// Represents an array data structure.
	/// </summary>
	[ImmutableObject(true)]
	public class Array : IEnumerable
	{
        #region Array Members

        #region Instance Fields

        // The length of the array.
        private int length;

        // The head node of the random access list.
        private RalTopNode head;

        #endregion

        #region Construction

        /// <summary>
        /// Initialize an instance of the Array class with the specified array 
        /// length.
        /// </summary>
        /// <param name="length">
        /// The length of the array.
        /// </param>
        public Array(int length)
        {
            // Precondition.
            if(length < 0)
            {
                throw new ArgumentOutOfRangeException("length", length,
                    "Array length out of range.");
            }

            this.length = length;

            int n = length;
            int exponent;
            int count;

            head = null;

            /*
             * The following algorithm creates the trees for the array. The
             * trees have the form of a random access list.
             */

            // While there are still nodes to create.
            while(n > 0)
            {
                // Get the log based 2 of the number of nodes.
                exponent = (int)Math.Log(n, 2);

                // Get the number of nodes for each subtree.
                count = ((int)Math.Pow(2, exponent) - 1) / 2;

                // Create the top node representing the subtree.
                head = new RalTopNode(
                    new RalTreeNode(
                        null, 
                        CreateSubTree(count), 
                        CreateSubTree(count)),
                    head);

                // Get the remaining number of nodes to create.
                n -= head.Root.Count;
            }            
        }

        /// <summary>
        /// Initializes a new instance of the Array class with the specified 
        /// head of the random access list and the length of the array.
        /// </summary>
        /// <param name="head">
        /// The head of the random access list.
        /// </param>
        /// <param name="length">
        /// The length of the array.
        /// </param>
        private Array(RalTopNode head, int length)
        {
            this.head = head;
            this.length = length;            
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the value of the specified element in the current Array. 
        /// </summary>
        /// <param name="index">
        /// An integer that represents the position of the Array element to 
        /// get. 
        /// </param>
        /// <returns>
        /// The value at the specified position in the Array.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// index is outside the range of valid indexes for the current Array.
        /// </exception>
        public object GetValue(int index)
        {
            // Preconditions.
            if(index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(
                    "Index out of range.");
            }

            return head.GetValue(index);            
        }

        /// <summary>
        /// Sets the specified element in the current Array to the specified 
        /// value.
        /// </summary>
        /// <param name="value">
        /// The new value for the specified element. 
        /// </param>
        /// <param name="index">
        /// An integer that represents the position of the Array element to set. 
        /// </param>
        /// <returns>
        /// A new array with the element at the specified position set to the 
        /// specified value.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// index is outside the range of valid indexes for the current Array.
        /// </exception>
        public Array SetValue(object value, int index)
        {
            // Preconditions.
            if(index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(
                    "Index out of range.");
            }

            return new Array(head.SetValue(value, index), Length);
        }

        // Creates subtrees within the random access list.
        private RalTreeNode CreateSubTree(int count)
        {
            RalTreeNode result = null;

            if(count > 0)
            {
                int c = count / 2;

                result = new RalTreeNode(
                    null,
                    CreateSubTree(c),
                    CreateSubTree(c));
            }

            return result;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets an integer that represents the total number of elements in all 
        /// the dimensions of the Array.
        /// </summary>
        public int Length
        {
            get
            {
                return length;
            }
        }

        #endregion

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an IEnumerator for the Array.
        /// </summary>
        /// <returns>
        /// An IEnumerator for the Array.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            return new RalEnumerator(head, length);
        }

        #endregion
    }
}

/*
 * Created by: Leslie Sanford 
 * 
 * Last modified: 02/23/2005
 * 
 * Contact: jabberdabber@hotmail.com
 */

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Sanford.Collections.Immutable
{
	/// <summary>
	/// Represents the top nodes in a RandomAccessList.
	/// </summary>
	[ImmutableObject(true)]
	internal class RalTopNode
	{
        #region RalTopNode Members

        #region Instance Fields

        // The root of the tree the top node represents.
        private readonly RalTreeNode root;

        // The next top node in the list.
        private readonly RalTopNode nextNode;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the RalTopNode with the specified 
        /// root of the tree this node represents and the next top node in the
        /// list.
        /// </summary>
        /// <param name="root">
        /// The root node of the tree this top node represents.
        /// </param>
        /// <param name="nextNode">
        /// The next top node in the list.
        /// </param>
		public RalTopNode(RalTreeNode root, RalTopNode nextNode)
		{
            Debug.Assert(root != null);

            this.root = root;
            this.nextNode = nextNode;
		}

        #endregion

        #region Methods

        /// <summary>
        /// Gets the value at the specified element in the random access list.
        /// </summary>
        /// <param name="index">
        /// An integer that represents the position of the random access list 
        /// element to get. 
        /// </param>
        /// <returns>
        /// The value at the specified position in the random access list.
        /// </returns>
        public object GetValue(int index)
        {            
            int i = index;
            RalTopNode currentNode = this;

            // Find the top node containing the specified element.
            while(i >= currentNode.Root.Count)
            {
                i -= currentNode.Root.Count;
                currentNode = currentNode.NextNode;

                Debug.Assert(currentNode != null);
            }

            return currentNode.Root.GetValue(i);
        }

        /// <summary>
        /// Sets the specified element in the current random access list to the 
        /// specified value.
        /// </summary>
        /// <param name="value">
        /// The new value for the specified element. 
        /// </param>
        /// <param name="index">
        /// An integer that represents the position of the random access list  
        /// element to set. 
        /// </param>
        /// <returns>
        /// A new random access list top node with the element at the specified 
        /// position set to the specified value.
        /// </returns>
        public RalTopNode SetValue(object value, int index)
        {
            RalTopNode result;

            // If the element is in the tree represented by the current top 
            // node.
            if(index < Root.Count)
            {
                // Descend into the tree.
                result = new RalTopNode(
                    root.SetValue(value, index), 
                    NextNode);
            }
            // Else the element is further along in the list.
            else
            {
                // Move to the next top node.
                result = new RalTopNode(
                    root, 
                    NextNode.SetValue(value, index - Root.Count));
            }

            return result;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the root node represented by the top node.
        /// </summary>
        public RalTreeNode Root
        {
            get
            {
                return root;
            }
        }
        
        /// <summary>
        /// Gets the next top node in the random access list.
        /// </summary>
        public RalTopNode NextNode
        {
            get
            {
                return nextNode;
            }
        }

        #endregion

        #endregion
	}
}
